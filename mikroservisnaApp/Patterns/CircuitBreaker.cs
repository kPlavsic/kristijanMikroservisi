namespace mikroservisnaApp.Patterns
{
    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }

    public class CircuitBreaker
    {
        private object _lock = new object(); 
        private readonly int _failureThreshold;  
        private readonly TimeSpan _openDuration; 
        private DateTime _lastFailureTime;       
        private int _failureCount;               
        private CircuitBreakerState _state;      

        public CircuitBreaker(int failureThreshold, TimeSpan openDuration)
        {
            _failureThreshold = failureThreshold;
            _openDuration = openDuration;
        }

        
        public CircuitBreakerState State
        {
            get
            {
                lock (_lock)
                {
                    if (_state == CircuitBreakerState.Open && (DateTime.UtcNow - _lastFailureTime) > _openDuration)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                    }
                }
                return _state;
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            //Ako je Open — odmah baci grešku, ne pokušavaj čak ni da zoveš API. Ovo štedi vreme i resurse.
            if (State == CircuitBreakerState.Open)
            {
                throw new CircuitBreakerOpenException("CircuitBreaker Open Exception");
            }

            try
            {
                var result = await action();

                lock (_lock)
                {
                    _failureCount = 0;
                    _state = CircuitBreakerState.Closed;
                }

                return result;
            }
            catch (Exception)
            {
                lock (_lock)
                {
                    _failureCount++;
                    _lastFailureTime = DateTime.UtcNow;

                    if (_state == CircuitBreakerState.HalfOpen)
                    {
                        _state = CircuitBreakerState.Open; // testiranje nije prošlo, nazad na Open
                    }
                    if (_failureCount > _failureThreshold)
                    {
                        _state = CircuitBreakerState.Open; // previše grešaka, otvori se
                    }
                }
                throw;
            }
        }
    }

    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string? message) : base(message)
        {
        }
    }
}