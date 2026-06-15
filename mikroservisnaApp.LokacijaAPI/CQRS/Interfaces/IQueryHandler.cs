namespace mikroservisnaApp.LokacijaAPI.CQRS.Interfaces
{
    public interface IQueryHandler<TQuery, TResult>
    {
        Task<TResult> Handle(TQuery query);
    }
}