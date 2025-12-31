namespace ScePSP.Utils
{
    public interface IArgumentReader
    {
        int LoadInteger();
        float LoadFloat();
        long LoadLong();
        string LoadString();
    }
}