namespace mikroservisnaApp.LokacijaAPI.CQRS.Interfaces
{
    public interface ICommandHandler<TCommand, TResult>
    {
        Task<TResult> Handle(TCommand command);
    }
}