using System;
using System.Threading.Tasks;
using Akka.Actor;

namespace Dsu.Common.Utilities.ExtensionMethods
{
    public static class EmAkka
    {
        private static ActorSystem ActorSystem { get { return CommonApplication.ActorSystem; } }
        public static void TellAcknowledge(this IActorRef sender, IActorRef self)
        {
            if (sender == null || self == null)
                return;

            var deadLetter = ActorSystem.DeadLetters;
            if (sender == deadLetter)
                Console.WriteLine("DeadLetter");
            if (sender != deadLetter && self != deadLetter && sender != self)
                sender.Tell(Task.Delay(0), self);
            else
                Console.WriteLine("Skip acknowledge");
        }
    }

    /*
     * ActorSystem.AwaitTermination()   : hangs
     * ActorSystem.Shutdown()
     * ActorSystem.Stop(SomeActor)
     */
}
