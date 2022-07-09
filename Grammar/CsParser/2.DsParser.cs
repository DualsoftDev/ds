using System;
using System.Collections.Generic;
using System.Linq;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

//using ParseTree = Antlr4.Runtime.RuleContext;

namespace DsParser
{

    class DsParser
    {
        public static dsParser FromDocument(string text)
        {
            var str = new AntlrInputStream(text);
            System.Console.WriteLine(text);
            var lexer = new dsLexer(str);
            var tokens = new CommonTokenStream(lexer);
            var parser = new dsParser(tokens);

            //var listener_lexer = new ErrorListener<int>();
            //var listener_parser = new ErrorListener<IToken>();
            return parser;
        }

        public static List<T> enumerateChildren<T>(IParseTree from, bool includeMe=true, Func<IParseTree, bool> predicate = null ) where T : IParseTree
        {
            var result = new List<T>();
            enumerateChildrenHelper(result, from, includeMe, predicate);
            return result;


            void enumerateChildrenHelper(List<T> rslt, IParseTree frm, bool incMe, Func<IParseTree, bool> pred)
            {
                bool ok(IParseTree t)
                {
                    if (pred != null)
                        return pred(t);
                    return true;
                }

                if (incMe && ok(frm))
                    rslt.Add((T)frm);
                for (int index = 0; index < frm.ChildCount; index++)
                    enumerateChildrenHelper(rslt, frm.GetChild(index), true, ok);
            }
        }

        public static IEnumerable<IParseTree> enumerateParents(IParseTree from, bool includeMe = true, Func<IParseTree, bool> predicate = null)
        {
            bool ok(IParseTree t)
            {
                if (predicate != null)
                    return predicate(t);
                return true;
            }

            if (includeMe && ok(from))
                yield return from;

            foreach (var p in enumerateParents(from.Parent, true, ok))
                yield return p;
        }


        public static IParseTree findFirstChild(IParseTree from, Func<IParseTree, bool> predicate, bool includeMe = true)
        {
            foreach (var c in DsParser.enumerateChildren<IParseTree>(from, includeMe))
            {
                if (predicate(c))
                    return c;
            }

            return null;
        }

        public static IParseTree findFirstAncestor(IParseTree from, Func<IParseTree, bool> predicate, bool includeMe=true)
        {
            foreach (var c in DsParser.enumerateParents(from, includeMe))
            {
                if (predicate(c))
                    return c;
            }

            return null;
        }



        public static ParserResult getParseResult(dsParser parser)
        {
            var listener = new AllListener();
            ParseTreeWalker.Default.Walk(listener, parser.program());
            return listener.r;
        }

        /**
         * parser tree 상의 모든 node (rule context, terminal node, error node) 을 반환한다.
         * @param text DS Document (Parser input)
         * @returns
         */
        public static List<IParseTree> getAllParseTrees(dsParser parser)
        {
            ParserResult r = getParseResult(parser);
            //return [].concat.apply([], [r.rules, r.terminals, r.errors]);


            return r.rules.Cast<IParseTree>()
                .Concat(r.terminals.Cast<IParseTree>())
                .Concat(r.errors.Cast<IParseTree>())
                .ToList()
                ;
        }


        /**
         * parser tree 상의 모든 rule 을 반환한다.
         * @param text DS Document (Parser input)
         * @returns
         */
        public static List<ParserRuleContext> getAllParseRules(dsParser parser)
        {
            ParserResult r = getParseResult(parser);
            return r.rules;
        }
    }
}
