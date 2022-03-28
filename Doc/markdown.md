Markdown 사용법입니다.


<!-- Headings -->
Ctrl-k v : preview : VS code 에서 편집할 때, markdown 확장을 설치해야 사용가능합니다.
- [Mastering MarkDown](https://guides.github.com/features/mastering-markdown/)
- [Markdown: Syntax](https://daringfireball.net/projects/markdown/syntax)
- [Emoji](https://github.com/ikatyang/emoji-cheat-sheet/blob/master/README.md)
- [GITHUBF L AV O R E DMARKDOWN](https://guides.github.com/pdfs/markdown-cheatsheet-online.pdf)

# Heading 1
## Heading 2
### Heading 3
#### Heading 4
##### Heading 5
###### Heading 6

*italic* text
_italic_ text
**strong** text
__strong__ text
~lower~ text
^upper^ text

~~This text~~ is strike through

---

triple underscore
___
* * *
---
*****
---

  
to escape, use backslash
\*normal\* text

[Dualsoft](http://dualsoft.com)


별표, 더하기, 하이픈 사용가능
<!-- UL -->
* item1
* item2
  * item1
  * item2

<!-- OL -->
1. item1
2. item2
3. item3
4. item4

<!-- Inline code block -->
`<p>This is a paragraphe</p>`

<a href='//www.microsoft.com/store/apps/9NKV1D43NLL3?cid=storebadge&ocid=badge'>
   <img src='https://assets.windowsphone.com/85864462-9c82-451e-9355-a3d5f874397a/English_get-it-from-MS_InvariantCulture_Default.png' alt='English badge' style='width: 284px; height: 104px;' width='284' height='104'/>
</a>



>이거는 인용구... > 로 시작하면 OK.  In HTML, there are two characters that demand special treatment: < and &. Left angle brackets are used to start tags; ampersands are used to denote HTML entities. If you want to use them as literal characters, you must escape them as entities, e.g. &lt;, and &amp;.




I get 10 times more traffic from [Google][] than from
[Yahoo][] or [MSN][].  [Yahoo][] : [Yahoo][] : [Yahoo][]

  [google]: http://google.com/        "Google"
  [yahoo]:  http://search.yahoo.com/  "Yahoo Search"
  [msn]:    http://search.msn.com/    "MSN Search"


## code

To indicate a span of code, wrap it with backtick quotes (`). Unlike a pre-formatted code block, a code span indicates code within a normal paragraph. For example:

Inline code
Use the `printf()` function.


A single backtick in a code span: `` ` ``

A backtick-delimited string in a code span: `` `foo` ``

### syntax highlight
```javascript
function fancyAlert(arg) {
  if(arg) {
    $.facebox({div:'#foo'})
  }
}
```




![Markdown Logo](https://markdown-here.com/img/icon256.png)




```
#!/bin/bash
npm install
npm start
```


```javascript
function add(num1, num2) {
    return num1 + num2;
}
```

```python
def add(num1, num2) {
    return num1 + num2;
}
```

```fsharp
let add x y = fun x -> fun y -> x + y
```
<!-- tables -->
| Name     | Email  |
| ----- | ---- |
| kwak | kwak@dualsoft.com|
| ahn | ahn@dualsoft.com|



<!-- task lists -->
* [x] task 1 : completed
* [ ] task 3
* [ ] 


##### Issue references within a repository

Any number that refers to an Issue or Pull Request will be automatically converted into a link.

#1
mojombo#1
mojombo/github-flavored-markdown#1

## Emoji
:kissing:
:100:
:ok_hand:
:v:
:+1:
:)
:(
:-)

<!-- LaTeX -->
```math
a^2 = b^2 + c^2
\\
SE = \frac{\sigma}{\sqrt{n}}
```

The syntax for inline latex is $`\sqrt{2}`$.

The syntax for inline latex is $\sqrt{2}$.


