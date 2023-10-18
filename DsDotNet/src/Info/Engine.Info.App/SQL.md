WITH ChangeEvents AS (
    SELECT
        [fqdn],
        [tagKind],
        [at],
        [value],
        LAG([value]) OVER (PARTITION BY [fqdn], [tagKind] ORDER BY [at]) AS prevValue,
        LAG([at]) OVER (PARTITION BY [fqdn], [tagKind] ORDER BY [at]) AS prevAt
    FROM
        [vwLog]
    WHERE
        [fqdn] = 'mySys.F2.A' -- 여기에 원하는 fqdn 값을 입력
        AND [tagKind] = 11000 -- 여기에 원하는 tagKind 값을 입력
)

-- SELECT * FROM ChangeEvents;

-- select '2023-10-18 16:37:05.9412614' - '2023-10-18 16:37:06.195672' as t;

-- SELECT    datetime('2023-10-18 16:37:06.195672', 'unixepoch') - datetime('2023-10-18 16:37:05.9412614', 'unixepoch') AS t;


SELECT
    [fqdn],
    [tagKind],
    [at],
    prevValue,
    [value],
    prevAt,
    [at] - PrevAt AS AtDiff -- at과 PrevAt의 차이 계산
FROM
    ChangeEvents
WHERE
    [prevValue] = 1 AND [value] = 0
    ;