#### Logging
$n$ 개의 children 을 갖는 segment 의 상태 logging
- segment 갯수
    - $n$ : 전체 children segment 의 갯수
    - $r$ : 초기 상태 고정가 고정된 children 의 갯수
    - $n-r$ : 초기 상태 don't care

$n$ 개의 children 을 정해진 순서대로 나열하였을 때 $i$-th child 의 ON/OFF 값을 $b_i$ 라고 하면,
특정 시점에서의 모든 children 의 상태 값은 다음과 같이 표시할 수 있다.
$$\underbrace{
\overbrace{ b_1 b_2 \cdots  b_r}^{r} \overbrace{b_{r+1} \cdots b_n}^{n-r}
}_{n}   $$

- 시작 가능 상태 경우의 수 : $k=2^{n-r}$
    - $b_1 b_2 \cdots b_r$ 은 시작 가능 상태가 특정 값으로 고정되어 있다.  [origin.md](origin.md)
    - 초기 상태가 가변인 child 의 갯수 $n-r$ 개의 2진 조합 갯수가 시작점 갯수($k$)이다.
    case 1 : $b_1 b_2 \cdots b_r \overbrace{0 \cdots 0 0}^{n-r}$
    case 2 : $b_1 b_2 \cdots b_r 0 \cdots 0 1$
    case 3 : $b_1 b_2 \cdots b_r 0 \cdots 1 0$
    case 4 : $b_1 b_2 \cdots b_r 0 \cdots 1 1$
    $\cdots$
    case $k$ : $b_1 b_2 \cdots b_r 1 \cdots 1 1$

- 상태(2진)의 값 변환:
    - 2진 상태값 원주율 mapping function: $\phi$
    - 특정 시점의 모든 children 의 상태 값을 $[0..2\pi)$ 구간으로 mapping 한다.
    $\varphi_{0_i} = \phi(case_i) = \mathit{case}_i / 2^n \times 2\pi$
    $ 0 \le \varphi_{0_i} \lt 2\pi$

- 시작점 값 $\varphi_{0_i}$ ($k$ 개): 
    $\varphi_{0_i} = \phi(case_i) = \mathit{case}_i / 2^n \times 2\pi$
    where $ 1 \le i \le k$

- $\varphi_\theta : \theta$ 진행 중의 $\varphi$ 값

    - $\mathit{c\_state}_\theta = [b'_1 b'_2 \cdots  b'_n ]_\theta$
        - $b'_i : \theta$ 만큼 진행한 상태에서의 i-th segment($\rightarrow b_i$) 와 $\theta$-th segment($\rightarrow b_\theta$) 가 인과 관계가 없으면 0, 인과가 존재하면 $b_i$ 값        

        $$b'_i = \begin{cases}
                            0 & \textrm{for non causal} \\
                            b_i & \textrm{for causal}
                        \end{cases}
        $$

    - $\varphi_\theta = \phi(\mathit{c\_state}_\theta)$ 
