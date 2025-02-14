using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Dsu.Common.Utilities.ExtensionMethods;
using Dsu.PLC.Common;
using static LanguageExt.Prelude;


namespace Dsu.PLC.LS
{
    internal class LsChannelRequestExecutor : ChannelRequestExecutor
	{
		public LsConnection LsConnection { get { return (LsConnection) Connection; } }
        public LsChannelRequestExecutor(LsConnection connection, IEnumerable<TagBase> tags)
			: base(connection, tags)
		{
			Contract.Requires(tags.NonNullAny());
		}

		public override bool ExecuteRead()
		{
            throw new NotImplementedException();

            //var trial = LogixServices.TryReadLogixData(LsConnection);
            //return match(trial,
            //    Succ: v => true,
            //    Fail: ex => false
            //   );
        }
	}
}
