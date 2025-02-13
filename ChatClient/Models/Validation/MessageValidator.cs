using System.Text.RegularExpressions;
using ChatClient.Enums;

namespace ChatClient.Models.Validation;

public class MessageValidator
{
    private class StringValidationItem
    {
        private int MaximalLength { get; set; }
        private Regex Regex { get; set; }

        public StringValidationItem(int maximalLength, string regex)
        {
            MaximalLength = maximalLength;
            Regex = new Regex(regex);
        }
        
        public bool ValidateItem(string value)
        {
            return value.Length <= MaximalLength && Regex.IsMatch(value);
        }
    }
    
    private readonly IDictionary<MessageArguments, StringValidationItem> validationItems = new Dictionary<MessageArguments, StringValidationItem>
    {
        {MessageArguments.UserName, new(20, "^[A-z0-9-]+$")},
        {MessageArguments.DisplayName, new(20, @"^[\x20-\x7E]+$")},
        {MessageArguments.ChannelId, new(20, "^[A-z0-9.-]+$")},
        {MessageArguments.MessageContent, new(1400, @"^[\x20-\x7E ]+$")},
        {MessageArguments.Secret, new(128, @"^[A-z0-9-]+$")}
    };
    
    public bool IsValid(Message message)
    {
        foreach (var entry in message.Arguments)
        {
            if (validationItems.TryGetValue(entry.Key, out var validationItem))
            {
                if (!validationItem.ValidateItem((string)entry.Value))
                {
                    return false;
                }
            }
        }

        return true;
    }
    
    // public bool ValidateArgument(MessageArguments argument, string value)
    // {
    //     if (validationItems.TryGetValue(argument, out var validationItem))
    //     {
    //         return validationItem.ValidateItem(value);
    //     }
    //
    //     return false;
    // }
}