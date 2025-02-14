using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;

using Dual.Common.Base.FS;
using NUnit.Framework.Legacy;

namespace T
{
    public record Outer(int Id, string Name);
    public record Inner(int OuterId, string Value);

    [TestFixture]
    public class SQLJoinTest
    {
        [Test]
        public void InnerJoinTest()
        {
            var outerSeq = new List<Outer>
            {
                new Outer(1, "Outer1"),
                new Outer(2, "Outer2"),
                new Outer(3, "Outer3")
            };
            var innerSeq = new List<Inner>
            {
                new Inner(1, "Inner1"),
                new Inner(1, "Inner2"),
                new Inner(2, "Inner3")
            };


            var innerJoinResult =
                outerSeq.InnerJoin(
                    innerSeq,
                    o => o.Id,
                    i => i.OuterId,
                    (o, i) => (o.Name, i.Value));

            var naturalJoinResult =
                outerSeq.NaturalJoin(innerSeq,
                    (o, i) => o.Id == i.OuterId,  // 자연 조인을 위한 키 비교 함수
                    (o, i) => ValueTuple.Create(o.Name, i.Value));  // 결과 생성


            // 기대 결과
            var expectedInnerJoin = new List<(string, string)>
            {
                ("Outer1", "Inner1"),
                ("Outer1", "Inner2"),
                ("Outer2", "Inner3")
            };

            var expectedNaturalJoin = expectedInnerJoin;

            // Inner Join 테스트
            CollectionAssert.AreEqual(expectedInnerJoin, innerJoinResult, "Inner join 결과가 예상과 다릅니다.");

            // Natural Join 테스트
            CollectionAssert.AreEqual(expectedNaturalJoin, naturalJoinResult, "Natural join 결과가 예상과 다릅니다.");
        }


    }
}
