
namespace AccountingManagement.Services
{
    public interface IMessageService
    {
        string GetMessage();
    }

    public class MessageService : IMessageService
    {
        public string GetMessage()
        {
            return "Hello from the Message Service";
        }
    }
}
