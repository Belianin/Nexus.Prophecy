using Telegram.Bot.Types.ReplyMarkups;

namespace Nexus.Prophecy.Worker.Telegram
{
    public class UserResponse
    {
        public string Text { get; set; }
        public IReplyMarkup Markup { get; set; }
        
        public static implicit operator UserResponse(string text)
        {
            return new UserResponse
            {
                Text = text
            };
        } 
    }
}