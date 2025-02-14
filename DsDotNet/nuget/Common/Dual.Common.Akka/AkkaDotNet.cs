using System;
using System.Configuration;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using Akka.Actor;
using log4net;

namespace Dual.Common.Akka
{
    public static class AkkaExt
    {
        static AkkaExt()
        {
            var timeout = int.Parse(ConfigurationManager.AppSettings["akkaAskTimeoutSeconds"] ??"10");
            _defaultTimeout = TimeSpan.FromSeconds(timeout);
        }
        public static IActorRef GetServerActor(this ActorSystem actorSystem, string actorPath, ILog logger)
        {
            try
            {
                var server =
                    actorSystem.ActorSelection(actorPath)
                    .ResolveOne(TimeSpan.FromSeconds(5))
                    .Result
                    ;
                return server;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to get actor {actorPath}.\r\n{ex}");
                throw;
            }
        }

        public static async Task<ActorIdentity> GetIdentityAsync(this ICanTell actor, TimeSpan timeout)
        {
            try
            {
                var guid = Guid.NewGuid();
                var identity = await actor.Ask<ActorIdentity>(new Identify(guid), timeout);
                Debug.Assert(identity.MessageId.ToString() == guid.ToString());
                return identity;
            }
            catch (Exception) { return null; }
        }

        public static async Task<bool> PingAsync(this ICanTell actor, TimeSpan timeout)
            => await GetIdentityAsync(actor, timeout) != null;

        public static ActorIdentity GetIdentity(this ICanTell actor) =>
            GetIdentityAsync(actor, TimeSpan.FromSeconds(2)).Result;
        public static bool Ping(this ICanTell actor) =>
            PingAsync(actor, TimeSpan.FromSeconds(2)).Result;

        /// will be overriden by "akkaAskTimeoutSeconds" @ app.Config
        static TimeSpan _defaultTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Timeout 을 고려한 ASK.  client side 에서 주로 이용
        /// </summary>
        public static Task<T> AskLimited<T>(this ICanTell canTell, object message)
            => canTell.Ask<T>(message, _defaultTimeout);


        /// <summary>
        /// Fire and don't forget
        /// Actor 에 Ask 보내는 것은 Akka 에서 추천하지 않고, 여러 문제점이 있는 바, Tell 을 사용하면서 해당 메시지에 대한
        /// 응답을 받았음을 보장하기 위한 helper method.
        /// </summary>
        /// <typeparam name="T">응답으로 받을 message 의 Type.  AmReply derivative 이어야 함.</typeparam>
        /// <param name="canTell">메시지를 보낼 actor</param>
        /// <param name="message">보낼 메시지.  ActorMessage derivative 이어야 함</param>
        /// <param name="recipient">보낸 메시지에 대한 응답을 받을 actor</param>
        /// <param name="recieveMessageSubject">recipient 가 메시지를 받을 때마다 OnNext 로 메시지를 받았음을 알려주어야 하는 Subject</param>
        /// <param name="logger"></param>
        /// <param name="timeSpan">이 시간 안에 응답을 반드시 받아야 함.</param>
        /// <param name="onReceived">성공적으로 응답을 받은 경우에 수행할 action.</param>
        /// <param name="onFaiulre">주어진 시간내에 응답을 못받은 경우에 수행할 action.</param>
        public static void TellAndWatch<T>(this ICanTell canTell, ActorMessage message, IActorRef recipient
            , Subject<object> recieveMessageSubject, ILog logger=null, TimeSpan? timeSpan=null
            , Action<T> onReceived=null, Action onFaiulre=null) where T : AmReply
        {
            var ts = timeSpan ?? TimeSpan.FromSeconds(10);
            var cts = new CancellationTokenSource();
            cts.Token.Register(() =>
            {
                // cts toke 이 주어진 시간 경과 후, cancel 됨
                var msg = $"Failed to receive {typeof(T)} message on {message.GetType()}, within time limit {ts}";
                Console.WriteLine(msg);
                logger?.Error(msg);
                onFaiulre?.Invoke();
            });

            cts.CancelAfter(ts);

            // Tell Core
            canTell.Tell(message, recipient);
            //

            IDisposable subscription = null;
            subscription =
                recieveMessageSubject
                    .OfType<T>()
                    .Where(m => m.QueryMessage.Guid == message.Guid)
                    .Subscribe(m =>
                    {
                        var msg = $"Got receive {m.GetType().Name} message on {message.GetType().Name}, within time limit {ts}";
                        Console.WriteLine(msg);
                        logger?.Debug(msg);

                        cts.Dispose();
                        subscription.Dispose();
                        onReceived?.Invoke(m);
                    });
        }

        public static string GetParentPathName(this IActorRef actor) => actor?.Path.Parent?.Address.ToString();
    }
}
