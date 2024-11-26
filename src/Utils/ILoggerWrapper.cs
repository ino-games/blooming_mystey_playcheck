namespace Service.LogicCommon.Utils
{


    public interface ILoggerWrapper
    {

        void Info(string message);
        void Debug(string message);
        void Error(string message);

    }
}