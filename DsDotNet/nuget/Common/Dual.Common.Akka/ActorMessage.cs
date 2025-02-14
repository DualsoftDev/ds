using System;

using Dual.Common.Core;

using Newtonsoft.Json;

namespace Dual.Common.Akka
{
    /// <summary>
    /// Typed actor message 의 가장 base class
    /// </summary>
    public abstract class ActorMessage
    {
        public Guid Guid { get; private set; }
        public object Message { get; private set; }
        public string IPAddress { get; set; }

        protected ActorMessage(Guid guid, object message)
        {
            Message = message;
            Guid = guid;
            IPAddress = NetEx.GetLocalIpAddress().ToString();
        }

        public ActorMessage(object message)
            : this(Guid.NewGuid(), message)
        {
        }

        [JsonConstructor]
        protected ActorMessage() : this(Guid.NewGuid(), null) { }

        public virtual string ToText() => $"{Guid.ToString().Substring(0, 8)} {Message?.ToString()}@{IPAddress}";
        public override string ToString() => $"{this.GetType().Name} {this.ToText()}";
    }

    /// <summary>
    /// actor 응답 message 의 가장 base class
    /// </summary>
    public abstract class AmReply
        : ActorMessage
    {
        protected AmReply(object message, ActorMessage query)
            : base(message)
        {
            QueryMessage = query;
        }
        protected AmReply() { }

        /// Reply 의 원본 질문 message
        public ActorMessage QueryMessage { get; set; }
    }


    /// <summary>
    /// actor 에러 message 의 가장 base class
    /// </summary>
    public class AmReplyError
        : AmReply
    {
        public AmReplyError(string exceptionMessage, ActorMessage query)
            : base(exceptionMessage, query)
        {
        }
        internal AmReplyError() { }
        public string ExceptionMessage => (string)base.Message;
    }

    public class AmSubscribe : ActorMessage { }

    public class AmReplySubscription : AmReply
    {
        public AmReplySubscription(AmSubscribe query)
            : base(null, query)
        { }
        public AmReplySubscription(object message, AmSubscribe query)
            : base(message, query)
        { }
        protected AmReplySubscription() { }
    }

    /// <summary>
    /// 극약 처방
    /// </summary>
    public class AmPoisonPill
        : ActorMessage
    {
    }

    public class AmPing : ActorMessage {}
    public class AmPong : AmReply
    {
        public AmPong(AmPing query)
            : base(null, query)
        {
        }
        private AmPong() {}
    }


    public class AmAction
        : ActorMessage
    {
        public Action Action { get; set; }
        public AmAction(Action action)
        {
            Action = action;
        }
        protected AmAction() {}
    }
}
