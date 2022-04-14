:smile: Welcome to the DS world  :smile:
# Example 2 

## Automatic door system :door:


 ![AAA](./ex2.dio.png)
 
  - action list 
    1. Open
    2. Close
    3. Detecting

```
 [Sys]door  = { Open <|> Close
               Detecting > Open
               (! Detecting) > Close
  }
```
