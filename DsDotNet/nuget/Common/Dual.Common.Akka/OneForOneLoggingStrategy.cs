using Akka.Actor;
using log4net;
using System;

namespace Dual.Common.Akka
{
    /// Actor 의 exception 발생시, logging 처리하기 위한 supervisor 전략.
    /// actor 에서 message hadling 시, exception 발생한 것을 catch 하는 것은 항상
    /// parent 가 존재할 때, parent 가 exception 을 우선 처리할 수 있으므로,
    public class OneForOneLoggingStrategy : OneForOneStrategy
    {
#if SIMPLE_IMPL
        public OneForOneLoggingStrategy(ILog logger)
            : base(reason =>
            {
                logger.Error($"Supervisor got {reason}");
                return Akka.Actor.SupervisorStrategy.DefaultDecider.Decide(reason);
            })
        {
            // do nothing
        }
#else
        ILog _logger;
        /// Actor 의 exception 발생시, logging 처리하기 위한 supervisor 전략.
        public OneForOneLoggingStrategy(ILog logger)
        {
            _logger = logger;
        }


        protected override Directive Handle(IActorRef child, Exception exception)
        {
            _logger.Error($"Supervisor got {exception} from {child.Path}");
            return base.Handle(child, exception);
        }
#endif
    }
}
