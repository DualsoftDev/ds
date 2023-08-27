using System;
using System.Collections.Generic;
using LanguageExt;

namespace Server.HW.Common
{
    public abstract class ChannelRequestExecutor
    {
        public ConnectionBase Connection { get; internal set; }
        public List<TagHW> Tags { get; } = new List<TagHW>();

        /// <summary> /// Channel 을 통해서 보낼 packet /// </summary> 
        public byte[] RequestPacket { get; internal set; }

        protected ChannelRequestExecutor(ConnectionBase connection, IEnumerable<TagHW> tags)
        {
            Connection = connection;
            Tags.AddRange(tags);
        }


        public Try<bool> TryExecuteRead()
        {
            return () => ExecuteRead();
        }

        /// <summary>
        /// Channel 을 통해서 packet 을 전송하고, 받은 packet 결과를 분석해서 Tags 에 저장.  각 HW maker 에 따라 다르게 구현한다.
        /// </summary>
        public abstract bool ExecuteRead();
	}
}
