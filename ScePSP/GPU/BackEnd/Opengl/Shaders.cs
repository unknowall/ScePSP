namespace ScePSP.Core.GpuBackEnd.OpenGL
{
    public class Shaders
    {
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

			#define LIGHT_MODEL_COLOR_CONTROL_SEPARATE_SPECULAR_COLOR 1
			#define LIGHT_MODEL_COLOR_CONTROL_SINGLE_COLOR 0
			#define MAX_LIGHTS 4

			struct Light {
				vec4 ambient;
				vec4 diffuse;
				vec4 specular;
				vec4 position;       // w=0: 方向光, w=1: 点光源/聚光灯
				vec3 spotDirection;
				float spotExponent;
				float spotCutoff;
				float constantAttenuation;
				float linearAttenuation;
				float quadraticAttenuation;
				bool enabled;
				int type; // Directional = 0, PointLight = 1, SpotLight = 2
			};

			layout(std140) uniform LightBlock {
				vec4 materialEmission;
				vec4 materialAmbient;
				vec4 materialDiffuse;
				vec4 materialSpecular;
				float materialShininess;
				vec4 lightModelAmbient;
				int lightModelColorControl;
				int MaterialColorComponents; // Ambient = 1, Diffuse = 2, Specular = 4
				Light lights[MAX_LIGHTS];
			};

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
				Light l = lights[lightIdx];
				if (!l.enabled) return vec4(0.0);

				vec4 result = vec4(0.0);
				vec3 L = vec3(0.0);
				float attenuation = 1.0;

				// 根据类型计算光向量 L 和衰减
				if (l.type == 0) {
					// 方向光 (Directional)
					L = normalize(l.position.xyz);
				} else {
					// 点光源 (Point) 或 聚光灯 (Spot)
					L = l.position.xyz - worldPos;
					float distance = length(L);
					L = normalize(L);

					attenuation = 1.0 / (l.constantAttenuation + 
					                     l.linearAttenuation * distance + 
					                     l.quadraticAttenuation * distance * distance);

					if (l.type == 2) {
						// 聚光灯 (SpotLight)
						if (l.spotCutoff < 180.0) {
							vec3 spotDir = normalize(l.spotDirection);
							float spotFactor = dot(-L, spotDir);
							if (spotFactor < cos(radians(l.spotCutoff))) {
								attenuation = 0.0;
							} else {
								spotFactor = pow(spotFactor, l.spotExponent);
								attenuation *= spotFactor;
							}
						}
					}
				}

				if (attenuation <= 0.0) return vec4(0.0);

				// Ambient = 1, Diffuse = 2, Specular = 4
				int comps = MaterialColorComponents;

				// 环境光分量
				if ((comps & 1) != 0) {
					result += l.ambient * materialAmbient;
				}

				// 漫反射分量
				if ((comps & 2) != 0) {
					float NdotL = max(dot(normal, L), 0.0);
					result += l.diffuse * materialDiffuse * NdotL;
				}

				// 镜面反射分量
				if ((comps & 4) != 0 && materialShininess > 0.0) {
					float NdotL = max(dot(normal, L), 0.0);
					if (NdotL > 0.0) {
						vec3 R = reflect(-L, normal);
						float RdotV = max(dot(R, viewDir), 0.0);
						float specularFactor = pow(RdotV, materialShininess);
						result += l.specular * materialSpecular * specularFactor;
					}
				}

				return result * attenuation;
			}

			vec4 calculateLighting(vec3 normal, vec3 worldPos, vec3 viewDir) {
				vec4 finalColor = vec4(0.0);

				// 发射光
				finalColor += materialEmission;

				// 全局环境光
				if ((MaterialColorComponents & 1) != 0) {
					finalColor += materialAmbient * lightModelAmbient;
				}

				// 累加光源
				for (int i = 0; i < MAX_LIGHTS; i++) {
					finalColor += calculateSingleLight(i, normal, viewDir, worldPos);
				}

				// 分离模式单独计算并叠加镜面光
				vec4 specularSeparate = vec4(0.0);
				if (lightModelColorControl == LIGHT_MODEL_COLOR_CONTROL_SEPARATE_SPECULAR_COLOR) {
					// 只有当材质组件包含镜面反射时才计算
					if ((MaterialColorComponents & 4) != 0) {
						for (int i = 0; i < MAX_LIGHTS; i++) {
							Light l = lights[i];
							if (!l.enabled) continue;

							vec3 L = vec3(0.0);
							float attenuation = 1.0;

							// 复制光照逻辑用于计算纯镜面分量
							if (l.type == 0) {
								L = normalize(l.position.xyz);
							} else {
								L = l.position.xyz - worldPos;
								float distance = length(L);
								L = normalize(L);
								attenuation = 1.0 / (l.constantAttenuation + 
								                     l.linearAttenuation * distance + 
								                     l.quadraticAttenuation * distance * distance);

								if (l.type == 2 && l.spotCutoff < 180.0) {
									vec3 spotDir = normalize(l.spotDirection);
									float spotFactor = dot(-L, spotDir);
									if (spotFactor < cos(radians(l.spotCutoff))) {
										attenuation = 0.0;
									} else {
										spotFactor = pow(spotFactor, l.spotExponent);
										attenuation *= spotFactor;
									}
								}
							}

							if (attenuation > 0.0 && materialShininess > 0.0) {
								float NdotL = max(dot(normal, L), 0.0);
								if (NdotL > 0.0) {
									vec3 R = reflect(-L, normal);
									float RdotV = max(dot(R, viewDir), 0.0);
									float specularFactor = pow(RdotV, materialShininess);
									specularSeparate += l.specular * materialSpecular * specularFactor * attenuation;
								}
							}
						}
					}
					
					// 镜面分量叠加到最终颜色
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
						gl_FragColor.rgb = texColor.rgb * gl_FragColor.rgb;
						gl_FragColor.a = (tcc == GU_TCC_RGBA) ? (gl_FragColor.a * texColor.a) : texColor.a;
						if (lightenable) {
							gl_FragColor.rgb = gl_FragColor.rgb * litColor.rgb;
						}
					}
					else if (tfx == GU_TFX_DECAL) {
						if (tcc == GU_TCC_RGB) {
							gl_FragColor.rgb = texColor.rgb;
							if (lightenable) {
								gl_FragColor.rgb = gl_FragColor.rgb * litColor.rgb;
							}
							gl_FragColor.a = texColor.a;
						} else {
							gl_FragColor.rgb = texColor.rgb * gl_FragColor.rgb;
							if (lightenable) {
								gl_FragColor.rgb = gl_FragColor.rgb * litColor.rgb;
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
						gl_FragColor.rgb = texColor.rgb;
						if (lightenable) {
							gl_FragColor.rgb = gl_FragColor.rgb * litColor.rgb;
						}
						gl_FragColor.a = (tcc == GU_TCC_RGB) ? gl_FragColor.a : texColor.a;
					} 
					else if (tfx == GU_TFX_ADD) {
						gl_FragColor.rgb += texColor.rgb;
						if (lightenable) {
							gl_FragColor.rgb += texColor.rgb * litColor.rgb;
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

				if (!clearingMode && lightenable) {
					//gl_FragColor = litColor;
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
			
			uniform int TextureMode;

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

				if(TextureMode == 0){
					v_texCoords = (matrixTexture * vertexTexCoords).xy;

				} else if(TextureMode == 1){
					vec3 worldPos = v_worldPos;
					vec3 viewDir = normalize(cameraPos - worldPos);
					vec3 normal = normalize(v_normal.xyz);
					vec3 reflectVec = reflect(-viewDir, normal);
					v_texCoords = (reflectVec.xy * 0.5 + 0.5);

				} else if(TextureMode == 2){ //GU_POSITION
					v_texCoords = (matrixTexture * vertexPosition).xy;

				} else if(TextureMode == 3){ //GU_NORMAL
					v_texCoords = (matrixTexture * vertexNormal).xy;

				} else if(TextureMode == 4){ //GU_NORMALIZED_NORMAL
					vec4 normalizedNormal = normalize(vertexNormal);
					v_texCoords = (matrixTexture * normalizedNormal).xy;

				} else if(TextureMode == 5){ //GU_UV
					v_texCoords = (matrixTexture * vertexTexCoords).xy;
				}

			}
		";
    }
}
