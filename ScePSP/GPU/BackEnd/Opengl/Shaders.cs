namespace ScePSP.Core.GpuBackEnd.OpenGL
{
    public class Shaders
    {
        //language=c++
        static public string ShaderFrag = @"
			#extension GL_EXT_gpu_shader4 : enable

			#define GU_TFX_MODULATE  0
			#define GU_TFX_DECAL     1
			#define GU_TFX_BLEND     2
			#define GU_TFX_REPLACE   3
			#define GU_TFX_ADD       4
								    
			#define GU_TCC_RGB       0
			#define GU_TCC_RGBA      1
								    
			#define GU_NEVER         0
			#define GU_ALWAYS        1
			#define GU_EQUAL         2
			#define GU_NOTEQUAL      3
			#define GU_LESS          4
			#define GU_LEQUAL        5
			#define GU_GREATER       6
			#define GU_GEQUAL        7

			#define GU_CLEAR         0
			#define GU_AND           1
			#define GU_AND_REVERSE   2
			#define GU_COPY          3
			#define GU_AND_INVERTED  4
			#define GU_NOOP          5
			#define GU_XOR           6
			#define GU_OR            7
			#define GU_NOR           8
			#define GU_EQUIV         9
			#define GU_INVERTED      10
			#define GU_OR_REVERSE    11
			#define GU_COPY_INVERTED 12
			#define GU_OR_INVERTED   13
			#define GU_NAND          14
			#define GU_SET           15

			uniform vec4 materialEmission;    // 发射光
			uniform vec4 materialAmbient;     // 环境光
			uniform vec4 materialDiffuse;     // 漫反射
			uniform vec4 materialSpecular;    // 镜面反射
			uniform float materialShininess;  // 镜面高光指数
			uniform vec4 lightModelAmbient;
			uniform int lightModelColorControl;

			#define LIGHT_MODEL_COLOR_CONTROL_SEPARATE_SPECULAR_COLOR 1
			#define LIGHT_MODEL_COLOR_CONTROL_SINGLE_COLOR 0
			#define MAX_LIGHTS 4

			uniform bool lightEnableds[MAX_LIGHTS];
			uniform vec4 lightAmbient[MAX_LIGHTS];    // 光源环境光
			uniform vec4 lightDiffuse[MAX_LIGHTS];    // 光源漫反射
			uniform vec4 lightSpecular[MAX_LIGHTS];   // 光源镜面反射
			uniform vec4 lightPosition[MAX_LIGHTS];   // 光源位置 (w=1:点光源, w=0:方向光)
			uniform vec3 lightSpotDirection[MAX_LIGHTS]; // 聚光灯方向
			uniform float lightSpotExponent[MAX_LIGHTS]; // 聚光灯指数
			uniform float lightSpotCutoff[MAX_LIGHTS];   // 聚光灯截止角 (0-90, 180=无聚光)
			uniform float lightConstantAttenuation[MAX_LIGHTS];  // 常数衰减
			uniform float lightLinearAttenuation[MAX_LIGHTS];    // 线性衰减
			uniform float lightQuadraticAttenuation[MAX_LIGHTS]; // 二次衰减

			uniform bool lightenable;
			uniform vec4 uniformColor;
			uniform vec4 TEC; 

			uniform int tfx;
			uniform int tcc;

			uniform bool lopEnabled;
			uniform int lop;

			uniform bool hasPerVertexColor;
			uniform bool hasTexture;
			uniform bool clearingMode;

			uniform bool colorTest;

			// ALPHA TEST
			uniform bool alphaTest;
			uniform int alphaFunction;
			uniform int alphaValue;
			uniform int alphaMask;

			uniform sampler2D backtex;
			uniform sampler2D texture0;

			varying vec4 v_color;
			varying vec4 v_normal;
			varying vec2 v_texCoords;
			varying vec2 v_backtexCoords;
			varying vec3 v_worldPos;
			varying vec3 v_viewDir;

			ivec4 convertToByte(vec4 v) {
				return ivec4(v * 255.0);
			}

			vec4 convertToFloat(ivec4 v) {
				return vec4(v) / 255.0;
			}

			vec4 calculateSingleLight(int lightIdx, vec3 normal, vec3 viewDir, vec3 worldPos) {
				if (!lightEnableds[lightIdx]) return vec4(0.0);

				vec4 result = vec4(0.0);
				vec3 L = vec3(0.0);
				float attenuation = 1.0;

				if (lightPosition[lightIdx].w == 0.0) {
					// 方向光 (w=0)
					L = normalize(lightPosition[lightIdx].xyz);
				} else {
					// 点光源/聚光灯 (w=1)
					L = lightPosition[lightIdx].xyz - worldPos;
					float distance = length(L);
					L = normalize(L);

					attenuation = 1.0 / (lightConstantAttenuation[lightIdx] + 
					                     lightLinearAttenuation[lightIdx] * distance + 
					                     lightQuadraticAttenuation[lightIdx] * distance * distance);

					if (lightSpotCutoff[lightIdx] < 180.0) {
						vec3 spotDir = normalize(lightSpotDirection[lightIdx]);
						float spotFactor = dot(-L, spotDir);
						if (spotFactor < cos(radians(lightSpotCutoff[lightIdx]))) {
							attenuation = 0.0;
						} else {
							spotFactor = pow(spotFactor, lightSpotExponent[lightIdx]);
							attenuation *= spotFactor;
						}
					}
				}

				if (attenuation <= 0.0) return vec4(0.0);

				result += lightAmbient[lightIdx] * materialAmbient;

				float NdotL = max(dot(normal, L), 0.0);
				result += lightDiffuse[lightIdx] * materialDiffuse * NdotL;

				if (NdotL > 0.0 && materialShininess > 0.0) {
					vec3 R = reflect(-L, normal);
					float RdotV = max(dot(R, viewDir), 0.0);
					float specularFactor = pow(RdotV, materialShininess);
					result += lightSpecular[lightIdx] * materialSpecular * specularFactor;
				}

				return result * attenuation;
			}

			// 计算总光照
			vec4 calculateLighting(vec3 normal, vec3 worldPos, vec3 viewDir) {

				vec4 ambientGlobal = lightModelAmbient * materialAmbient;
				vec4 totalLight = ambientGlobal + materialEmission;

				for (int i = 0; i < MAX_LIGHTS; i++) {
					totalLight += calculateSingleLight(i, normal, viewDir, worldPos);
				}

				vec4 specularSeparate = vec4(0.0);
				if (lightModelColorControl == LIGHT_MODEL_COLOR_CONTROL_SEPARATE_SPECULAR_COLOR) {
					// 重新计算镜面反射分量（单独分离）
					for (int i = 0; i < MAX_LIGHTS; i++) {
						if (!lightEnableds[i]) continue;

						vec3 L = vec3(0.0);
						float attenuation = 1.0;

						if (lightPosition[i].w == 0.0) {
							L = normalize(lightPosition[i].xyz);
						} else {
							L = lightPosition[i].xyz - worldPos;
							float distance = length(L);
							L = normalize(L);
							attenuation = 1.0 / (lightConstantAttenuation[i] + 
							                     lightLinearAttenuation[i] * distance + 
							                     lightQuadraticAttenuation[i] * distance * distance);

							if (lightSpotCutoff[i] < 180.0) {
								vec3 spotDir = normalize(lightSpotDirection[i]);
								float spotFactor = dot(-L, spotDir);
								if (spotFactor < cos(radians(lightSpotCutoff[i]))) {
									attenuation = 0.0;
								} else {
									spotFactor = pow(spotFactor, lightSpotExponent[i]);
									attenuation *= spotFactor;
								}
							}
						}

						float NdotL = max(dot(normal, L), 0.0);
						if (NdotL > 0.0 && materialShininess > 0.0) {
							vec3 R = reflect(-L, normal);
							float RdotV = max(dot(R, viewDir), 0.0);
							float specularFactor = pow(RdotV, materialShininess);
							specularSeparate += lightSpecular[i] * materialSpecular * specularFactor * attenuation;
						}
					}
				}

				// 合并颜色（分离镜面反射时，镜面光不参与主颜色计算，最后叠加）
				vec4 finalColor = totalLight;
				if (lightModelColorControl == LIGHT_MODEL_COLOR_CONTROL_SEPARATE_SPECULAR_COLOR) {
					finalColor.rgb += specularSeparate.rgb;
				}

				finalColor = clamp(finalColor, 0.0, 1.0);

				finalColor.a = materialDiffuse.a;

				return finalColor;
			}

			void main() {

				vec4 litColor = vec4(1.0, 1.0, 1.0, 1.0);

				if (lightenable) {
					vec3 normal = normalize(v_normal.xyz);
					vec3 viewDir = normalize(v_viewDir);
					litColor = calculateLighting(normal, v_worldPos, viewDir);
				}

				if (hasPerVertexColor) {
					gl_FragColor = v_color;
				} else {
					gl_FragColor = uniformColor;
				}

				if (!clearingMode && hasTexture) {
					vec4 texColor = texture2D(texture0, v_texCoords);

					if (alphaTest) {
						int alphaInt = int(texColor.a * 255.0) & alphaMask;
						if (alphaFunction == GU_NEVER   ) { discard; }
						else if (alphaFunction == GU_EQUAL   ) { if (!(alphaInt == alphaValue)) { discard; return; } }
						else if (alphaFunction == GU_NOTEQUAL) { if (!(alphaInt != alphaValue)) { discard; return; } }
						else if (alphaFunction == GU_LESS    ) { if (!(alphaInt <  alphaValue)) { discard; return; } }
						else if (alphaFunction == GU_LEQUAL  ) { if (!(alphaInt <= alphaValue)) { discard; return; } }
						else if (alphaFunction == GU_GREATER ) { if (!(alphaInt >  alphaValue)) { discard; return; } }
						else if (alphaFunction == GU_GEQUAL  ) { if (!(alphaInt >= alphaValue)) { discard; return; } }
					}

					if (tfx == GU_TFX_MODULATE) {
						if (lightenable) {
							gl_FragColor.rgb = texColor.rgb * gl_FragColor.rgb * litColor.rgb;
						} else {
							gl_FragColor.rgb = texColor.rgb * gl_FragColor.rgb;
						}
						gl_FragColor.a = (tcc == GU_TCC_RGBA) ? (gl_FragColor.a * texColor.a) : texColor.a;
					} 
					else if (tfx == GU_TFX_DECAL) {
						if (tcc == GU_TCC_RGB) {
							if (lightenable) {
								gl_FragColor.rgb = texColor.rgb * litColor.rgb;
							} else {
								gl_FragColor.rgb = texColor.rgb;
							}
							gl_FragColor.a = texColor.a;
						} else {
							if (lightenable) {
								gl_FragColor.rgb = texColor.rgb * gl_FragColor.rgb * litColor.rgb;
							} else {
								gl_FragColor.rgb = texColor.rgb * gl_FragColor.rgb;
							}
							gl_FragColor.a = texColor.a;
						}
					} 
					else if (tfx == GU_TFX_BLEND) {
						gl_FragColor.rgba = mix(texColor, gl_FragColor, 0.5);
						if (lightenable) {
							gl_FragColor *= litColor;
						}
					} 
					else if (tfx == GU_TFX_REPLACE) {
						if (lightenable) {
							gl_FragColor.rgb = texColor.rgb * litColor.rgb;
						} else {
							gl_FragColor.rgb = texColor.rgb;
						}
						gl_FragColor.a = (tcc == GU_TCC_RGB) ? gl_FragColor.a : texColor.a;
					} 
					else if (tfx == GU_TFX_ADD) {
						if (lightenable) {
							gl_FragColor.rgb += texColor.rgb * litColor.rgb;
						} else {
							gl_FragColor.rgb += texColor.rgb;
						}
						gl_FragColor.a = (tcc == GU_TCC_RGB) ? gl_FragColor.a : (texColor.a * gl_FragColor.a);
					} 
					else {
						gl_FragColor = vec4(1, 0, 1, 1);
					}
				}

				if (lopEnabled) {
					ivec4 s = convertToByte(gl_FragColor);
					ivec4 d = convertToByte(texture2D(backtex, v_backtexCoords));
					ivec4 o = ivec4(0x77);

					if (lop == GU_CLEAR        ) o = ivec4(0x00);
					else if (lop == GU_AND          ) o = s & d;
					else if (lop == GU_AND_REVERSE  ) o = s & ~d;
					else if (lop == GU_COPY         ) o = s;
					else if (lop == GU_AND_INVERTED ) o = ~s & d;
					else if (lop == GU_NOOP         ) o = d;
					else if (lop == GU_XOR          ) o = s ^ d;
					else if (lop == GU_OR           ) o = s | d;
					else if (lop == GU_NOR          ) o = ~(s | d);
					else if (lop == GU_EQUIV        ) o = ~(s ^ d);
					else if (lop == GU_INVERTED     ) o = ~d;
					else if (lop == GU_OR_REVERSE   ) o = s | ~d;
					else if (lop == GU_COPY_INVERTED) o = ~s;
					else if (lop == GU_OR_INVERTED  ) o = ~s | d;
					else if (lop == GU_NAND         ) o = ~(s & d);
					else if (lop == GU_SET          ) o = ivec4(0xFF);

					gl_FragColor = convertToFloat(o);
				}

				if (!clearingMode && !hasTexture && lightenable) {
					gl_FragColor = gl_FragColor * litColor;
				}

				//if (colorTest) {
				//	discard; return;
				//}
			}
        ";

        static public string ShaderVert = @"
			uniform mat4 matrixWorldViewProjection;
			uniform mat4 matrixTexture;
			uniform mat4 matrixBones[8];
			uniform mat4 matrixWorld;
			uniform mat4 matrixView;
			uniform int weightCount;
			uniform bool hasReversedNormal;

			attribute vec4 vertexTexCoords;
			attribute vec4 vertexColor;
			attribute vec4 vertexNormal;
			attribute vec4 vertexPosition;
			attribute float vertexWeight0;
			attribute float vertexWeight1;
			attribute float vertexWeight2;
			attribute float vertexWeight3;
			attribute float vertexWeight4;
			attribute float vertexWeight5;
			attribute float vertexWeight6;
			attribute float vertexWeight7;

			varying vec4 v_color;
			varying vec2 v_texCoords;
			varying vec2 v_backtexCoords;
			varying vec4 v_normal;
			varying vec3 v_worldPos;
			varying vec3 v_viewDir;

			vec4 performSkinning(vec4 In) {
				if (weightCount == 0) {
					return In;
				}

				vec4 Out = vec4(0.0, 0.0, 0.0, 0.0);
				
				float totalWeight = 0.0;
				if (weightCount > 0) { totalWeight += vertexWeight0;
				if (weightCount > 1) { totalWeight += vertexWeight1;
				if (weightCount > 2) { totalWeight += vertexWeight2;
				if (weightCount > 3) { totalWeight += vertexWeight3;
				if (weightCount > 4) { totalWeight += vertexWeight4;
				if (weightCount > 5) { totalWeight += vertexWeight5;
				if (weightCount > 6) { totalWeight += vertexWeight6;
				if (weightCount > 7) { totalWeight += vertexWeight7;
				}}}}}}}}

				if (weightCount > 0) { Out += (matrixBones[0] * (vertexWeight0 / totalWeight)) * In;
				if (weightCount > 1) { Out += (matrixBones[1] * (vertexWeight1 / totalWeight)) * In;
				if (weightCount > 2) { Out += (matrixBones[2] * (vertexWeight2 / totalWeight)) * In;
				if (weightCount > 3) { Out += (matrixBones[3] * (vertexWeight3 / totalWeight)) * In;
				if (weightCount > 4) { Out += (matrixBones[4] * (vertexWeight4 / totalWeight)) * In;
				if (weightCount > 5) { Out += (matrixBones[5] * (vertexWeight5 / totalWeight)) * In;
				if (weightCount > 6) { Out += (matrixBones[6] * (vertexWeight6 / totalWeight)) * In;
				if (weightCount > 7) { Out += (matrixBones[7] * (vertexWeight7 / totalWeight)) * In;
				}}}}}}}}

				return Out;
			}

			vec4 prepareNormal(vec4 normal) {
				vec4 n = hasReversedNormal ? -normal : normal;
				n.w = 0.0;
				return n;
			}

			void main() {

				vec4 skinnedPos = performSkinning(vertexPosition);
				vec4 skinnedNormal = performSkinning(prepareNormal(vertexNormal));

				gl_Position = matrixWorldViewProjection * skinnedPos;

				v_worldPos = (matrixWorld * skinnedPos).xyz;

				v_normal = matrixWorld * skinnedNormal;

				vec3 cameraPos = inverse(matrixView)[3].xyz;
				v_viewDir = normalize(cameraPos - v_worldPos);

				v_backtexCoords = (gl_Position.xy + vec2(1.0, 1.0)) / 2.0;
				v_color = vertexColor;
				v_texCoords = (matrixTexture * vertexTexCoords).xy;
			}
		";
    }
}