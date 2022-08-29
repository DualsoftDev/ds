
let a = query {
    for i in [1..10] do
        select i
}


query {
    for i in [1..10] do
    where (i < 5)
    count
}

query {
    for i in [1..10] do
    last
}