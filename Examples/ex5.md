:smile: Welcome to the DS world  :smile:
# Example 5

## Automatic door system (Advance I/F) :door:


 ![AAA](./png/ex5.dio.png)
 
  - action list 
    1. Open
    2. Close

```
 [Sys]door  = { Open <|> Close
               (Detect) > Open, CloseDelay(10Sec)  > Close
               CloseDelay(10Sec) <| Close

      Open = { Out1 ~ In1 }
      Close = { Out2 ~  }
  }
```
