namespace Engine.Parser.FS
{
    internal class ParserException : Exception
    {
        static string CreatePositionInfo(object ctx)   // RuleContext or IErrorNode
        {
            string getPosition(object ctx)
            {
                string fromToken(IToken token) => $"{token.Line}:{token.Column}";
                string fromErrorNode(IErrorNode errNode) =>
                    errNode switch
                    {
                        ErrorNodeImpl impl => fromToken(impl.Symbol),
                        _ => throw new Exception("ERROR"),
                    };

                return ctx switch
                {
                    ParserRuleContext prctx =>
                        prctx.Start switch
                        {
                            (CommonToken start) => fromToken(start),
                            _ => throw new Exception("ERROR"),
                        },
                    IErrorNode errNode => fromErrorNode(errNode),
                    _ => throw new Exception("ERROR"),
                };
            }

            string getAmbient(object ctx) =>
                ctx switch
                {
                    IParseTree pt => pt.GetText(),
                    _ => throw new Exception("ERROR"),
                };

            var posi = getPosition(ctx);
            var ambient = getAmbient(ctx);
            return $"{posi} near\r\n'{ambient}'";
        }
        public ParserException(string message, RuleContext ctx)
            : base($"{message} on {CreatePositionInfo(ctx)}")
        {
        }

        public ParserException(string message, IErrorNode errorNode)
            : base($"{message} on {CreatePositionInfo(errorNode)}")
        {
        }

        public ParserException(string message, int line, int column)
            : base($"{message} on {line}:{column}")
        {
        }


    }
}
