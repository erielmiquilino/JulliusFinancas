namespace Jullius.ServiceApi.Telegram.IntentHandlers;

public interface IIntentHandler
{
    IntentType HandledIntent { get; }
    Task<string> HandleAsync(ConversationState state);
    Task<string> HandleConfirmationAsync(ConversationState state, bool confirmed);
    List<string> GetMissingFields(ConversationState state);
    string BuildConfirmationMessage(ConversationState state);
}
