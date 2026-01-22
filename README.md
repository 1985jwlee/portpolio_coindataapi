# Coin Data API — Real-time Market Data Platform

> **외부 API 변동성을 흡수하고, 안정적인 내부 계약을 제공하는 데이터 플랫폼**

[![Platform](https://img.shields.io/badge/Platform-Financial%20Tech-green)](https://github.com/1985jwlee/portpolio_coindataapi)
[![Status](https://img.shields.io/badge/Status-Production%20Ready-success)](README.md)

---

## 📌 Executive Summary

**이 프로젝트가 증명하는 것:**

```
✓ 외부 데이터 소스의 불안정성을 내부에서 격리하는 설계
✓ 실시간 WebSocket 데이터를 REST API로 정규화하여 제공
✓ 거래소 API 장애 시에도 서비스 연속성 보장
✓ 기술 지표 계산 엔진의 모듈화 및 확장 가능한 구조
✓ 게임 서버 아키텍처 원칙의 금융 도메인 적용
```

**대상 독자**: 핀테크/트레이딩 백엔드 엔지니어, 데이터 플랫폼 아키텍트

**핵심 메시지**: 
> "외부 의존성의 변동성을 구조적으로 격리하고, 클라이언트에게 안정적인 계약을 제공합니다."

---

## 🎯 Why This Architecture?

### 실시간 금융 데이터 서비스의 구조적 문제

```
🚨 거래소 API 장애 → 전체 서비스 중단
🚨 외부 스키마 변경 → 클라이언트 코드 수정 필요
🚨 WebSocket 재연결 실패 → 데이터 유실
🚨 기술 지표 계산 로직 분산 → 일관성 부재
🚨 다중 거래소 통합 시 복잡도 폭증
```

### 핵심 판단

> **문제의 핵심은 외부 의존성과 내부 서비스의 결합도입니다.**

이 프로젝트는 위 문제를 **계층 분리**로 해결합니다.

---

## 🏗️ 3가지 핵심 설계 결정

### 1️⃣ External Schema → Internal Schema 정규화

```
[ Binance WebSocket ]
    ↓ 실시간 데이터 수신
[ BinanceSocketKlineManager ]
    ↓ 즉시 정규화
[ IBinanceKline (내부 표준) ]
    ↓ 기술 지표 계산
[ REST API ]
```

**판단 근거**:
- ✅ Binance API 스키마 변경이 클라이언트에 영향 없음
- ✅ 다른 거래소 추가 시 동일한 인터페이스 사용
- ✅ 기술 지표 계산 로직의 재사용성 확보

**실무 시나리오**:
```
Binance API 스키마 변경:
❌ 잘못된 설계: 모든 클라이언트 코드 수정
✅ 이 설계: Normalizer만 수정, 클라이언트 영향 없음
```

---

### 2️⃣ WebSocket → REST API 변환 계층

```
실시간 수집 계층:
[ WebSocket Stream ] → [ Queue ] → [ In-Memory Cache ]

안정적인 제공 계층:
[ HTTP REST API ] ← [ RxConcurrentDictionary ]
```

**판단 근거**:
- ✅ WebSocket 연결 불안정성을 HTTP 계층에서 흡수
- ✅ 클라이언트는 단순한 HTTP GET 요청만 수행
- ✅ 캐시된 데이터로 일시적 장애 대응

**트레이드오프**:
```
WebSocket 직접 제공:
- 장점: 최소 지연, 실시간성
- 단점: 연결 관리 복잡, 클라이언트 부담

REST API 제공 (선택):
- 장점: 단순한 통합, 안정성
- 단점: 약간의 지연 (1분 간격 갱신)
```

**결론**: 트레이딩 지표 분석 목적에는 1분 지연 허용 가능

---

### 3️⃣ 기술 지표 계산 엔진의 모듈화

```
[ ITechnicalData ] (인터페이스)
    ↓
[ TechnicalDataBase ] (추상 클래스)
    ↓
[ RSI | MACD | StochasticK | ... ] (구현체)
```

**구현 증거**:

```csharp
// 모든 기술 지표는 동일한 계약 준수
public interface ITechnicalData
{
    decimal mainValue { get; }
    StateAction GetStateAction();
    TrendStyle GetTrendStyle();
}

// 신규 지표 추가는 기존 코드 수정 없이 가능
public class NewIndicator : TechnicalDataBase
{
    public override StateAction GetStateAction()
    {
        // 지표별 시그널 로직
    }
}
```

**판단 근거**:
- ✅ 새로운 지표 추가 시 기존 코드 수정 불필요
- ✅ 각 지표의 계산 로직 독립적 유지
- ✅ 테스트 및 검증 용이

---

## 📊 시스템 아키텍처

### 전체 데이터 흐름

```
┌─────────────────────────────────────────────────────────┐
│                   External Data Source                   │
│                    Binance WebSocket                     │
└─────────────────────┬───────────────────────────────────┘
                      │ Raw Kline Data
                      ↓
┌─────────────────────────────────────────────────────────┐
│              BinanceSocketKlineManager                   │
│  - WebSocket 연결 관리                                    │
│  - 재연결 로직                                            │
│  - 데이터 Queue 관리                                       │
└─────────────────────┬───────────────────────────────────┘
                      │ IBinanceKline
                      ↓
┌─────────────────────────────────────────────────────────┐
│              Technical Indicator Engine                  │
│  RSI | MACD | Stochastic | ADX | CCI | ...              │
└─────────────────────┬───────────────────────────────────┘
                      │ Calculated Indicators
                      ↓
┌─────────────────────────────────────────────────────────┐
│           RxConcurrentDictionary<Interval, Data>        │
│  - In-Memory Cache                                       │
│  - 1분마다 갱신                                           │
└─────────────────────┬───────────────────────────────────┘
                      │ JSON Response
                      ↓
┌─────────────────────────────────────────────────────────┐
│                  DataWebServer (REST API)                │
│  /api/v1/summary                                         │
│  /api/v1/oscillators                                     │
│  /api/v1/moving_averages                                │
│  /api/v1/pivots                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 🔧 핵심 구현

### 1. WebSocket 데이터 수집 및 정규화

```csharp
public class BinanceSocketKlineManager : BinanceKlineManagerBase
{
    private Dictionary<string, Dictionary<KlineInterval, Queue<IBinanceStreamKlineData>>> receivedCandles;

    public override async Task<bool> Initialize()
    {
        foreach (var coinName in Defines.futureNames)
        {
            await socketconnect.UsdFuturesApi.ExchangeData
                .SubscribeToKlineUpdatesAsync(
                    coinName, 
                    Defines.binanceInterval, 
                    onMsg =>
                    {
                        lock (lockobj)
                        {
                            // WebSocket 수신 → Queue 적재
                            receivedCandles[coinName][interval].Enqueue(onMsg.Data);
                        }
                    }, 
                    false, 
                    ctx.Token);
        }
        return true;
    }

    public override async Task UpdateFutureCandles(string coinName, KlineInterval interval)
    {
        lock (lockobj)
        {
            // Queue → 내부 캔들 데이터 변환
            while (queue.Count > 0)
            {
                var result = queue.Dequeue();
                candleDatas[coinName][interval].Add(result.Data);
            }
        }
    }
}
```

**핵심 포인트**:
- WebSocket 수신과 데이터 처리를 분리
- Queue를 통한 비동기 처리
- Lock을 통한 동시성 제어

---

### 2. 기술 지표 계산 예시: RSI

```csharp
public class RSI : TechnicalDataBase
{
    private int duration;
    
    public RSI(int dur, List<IBinanceKline> candle) : base(candle)
    {
        duration = dur;
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        var output = new decimal[candleCount];
        
        // TALib.NETCore 라이브러리 사용
        var retCode = Core.Rsi(
            close, 0, candleCount - 1, 
            output, out _, out var outNbEle, duration);
        
        if (retCode == Core.RetCode.Success)
        {
            mainValue = output[outNbEle - 1];
        }
    }

    public override StateAction GetStateAction()
    {
        if (mainValue < 30 && GetTrendStyle() == TrendStyle.Upward) 
            return StateAction.Buy;
        if (mainValue > 70 && GetTrendStyle() == TrendStyle.Downward) 
            return StateAction.Sell;
        return StateAction.Neutral;
    }
}
```

**구현된 기술 지표**:
- Moving Averages: SMA, EMA, HullMA, VWMA
- Oscillators: RSI, Stochastic, MACD, CCI, Williams %R
- Trend: ADX, Ichimoku, Awesome Oscillator
- Momentum: Momentum, Bull Bear Power, Ultimate Oscillator
- Pivots: Classic, Fibonacci

---

### 3. REST API 제공

```csharp
public class DataWebServer
{
    private RxConcurrentDictionary<KlineInterval, Dictionary<string, string>> summarystrings;

    // 1분마다 모든 지표 사전 계산
    public async Task PrepareResponseData()
    {
        foreach (var interval in Defines.binanceInterval)
        {
            var summarydict = new Dictionary<string, string>();
            
            foreach (var ticker in Defines.futureNames)
            {
                await binanceConn.UpdateFutureCandles(ticker, interval);
                
                // 지표 계산 및 JSON 직렬화
                var summary = webserverStrings.Summary(ticker, interval);
                summarydict.Add(ticker, summary);
            }
            
            // 캐시 갱신
            summarystrings.TryRxAddOrUpdate(interval, summarydict);
        }
        
        await Task.Delay(TimeSpan.FromMinutes(1));
    }

    private async Task Summary(HttpContextBase ctx)
    {
        var symbol = ctx.Request.RetrieveQueryValue("symbol");
        var interval = ToolScript.ConvertToInterval(
            ctx.Request.RetrieveQueryValue("interval"));
        
        if (summarystrings.TryGetValue(interval, out var dict))
        {
            if (dict.TryGetValue(symbol, out var retstr))
            {
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(retstr);
            }
        }
    }
}
```

**핵심 포인트**:
- 사전 계산된 JSON 응답 제공 (지연 최소화)
- RxConcurrentDictionary로 동시성 안전 보장
- 캐시 기반 빠른 응답

---

## 🛡️ 장애 대응 설계

### 장애 영향도 매트릭스

| 장애 대상 | API 서비스 | 데이터 신선도 | 복구 전략 |
|----------|-----------|--------------|----------|
| Binance WebSocket | 🟢 정상 (캐시 제공) | 🟡 최대 1분 지연 | 자동 재연결 |
| 기술 지표 계산 실패 | 🟡 해당 지표만 누락 | 🟢 정상 | 로그 기록, 다음 주기 재시도 |
| In-Memory Cache | 🔴 서비스 중단 | 🔴 데이터 손실 | 서버 재시작 필요 |
| REST API 서버 | 🔴 서비스 중단 | 🟢 정상 (데이터는 수집 중) | 프로세스 재시작 |

### 장애 복구 전략

```
[ WebSocket 연결 끊김 ]
    ↓
[ 자동 재연결 시도 ]
    ↓
[ 성공 시: Queue에서 데이터 처리 재개 ]
[ 실패 시: 캐시된 데이터로 서비스 유지 (최대 1분 지연) ]
```

---

## 📈 API 사용 예시

### 1. 요약 데이터 조회

```bash
# 단일 심볼 조회
curl "http://localhost:9200/api/v1/summary?symbol=BTCUSDT&interval=1m"

# 응답 예시
{
  "overall_rating": "buy",
  "counted_actions": {
    "Buy": 15,
    "Neutral": 8,
    "Sell": 3
  },
  "indicators": [
    {
      "name": "rsi",
      "value": 45.23,
      "action": "Neutral"
    },
    {
      "name": "macd",
      "value": 123.45,
      "action": "Buy"
    }
  ]
}
```

### 2. 이동평균 조회

```bash
curl "http://localhost:9200/api/v1/moving_averages?symbol=ETHUSDT&interval=5m"

# 응답 예시
{
  "overall_rating": "strong buy",
  "counted_actions": {
    "Buy": 10,
    "Neutral": 2,
    "Sell": 1
  },
  "indicators": [
    {
      "name": "ema_7",
      "value": 2345.67,
      "action": "Buy"
    },
    {
      "name": "sma_20",
      "value": 2340.12,
      "action": "Buy"
    }
  ]
}
```

### 3. 피봇 포인트 조회

```bash
curl "http://localhost:9200/api/v1/pivots?symbol=ADAUSDT&interval=15m&period=14"

# 응답 예시
{
  "classic": {
    "P": 0.4523,
    "R1": 0.4556,
    "R2": 0.4589,
    "R3": 0.4622,
    "S1": 0.4490,
    "S2": 0.4457,
    "S3": 0.4424
  },
  "fibonacci": {
    "P": 0.4523,
    "R1": 0.4535,
    "R2": 0.4547,
    "R3": 0.4560,
    "S1": 0.4511,
    "S2": 0.4499,
    "S3": 0.4486
  }
}
```

---

## 🎓 게임 서버 아키텍처 원칙의 적용

이 프로젝트는 [메인 게임 아키텍처](https://github.com/1985jwlee/portpolio_main)의 설계 원칙이 금융 도메인에서도 동일하게 적용됨을 증명합니다.

| 원칙 | 게임 서버 적용 | Coin API 적용 |
|------|---------------|--------------|
| **외부 격리** | DB 장애 시 게임 진행 | 거래소 API 장애 시 캐시 제공 |
| **정규화 계층** | Event → DB Schema | External API → Internal Schema |
| **계약 안정성** | 운영 API 불변 | 클라이언트 API 불변 |
| **비동기 처리** | Kafka Event Stream | WebSocket → Queue → Cache |
| **장애 복구** | Hot/Cold Snapshot | In-Memory Cache + 재연결 |

---

## 💡 트레이드오프 & 의도적 선택

### ✅ 선택한 것

**안정성 우선**
- 외부 API 장애를 가정한 설계
- 캐시 기반 서비스 연속성 보장

**단순한 구조**
- In-Memory Cache (Redis 등 불필요)
- 단일 서버 구조 (수평 확장 시 고려)

**명확한 계층 분리**
- WebSocket 수집 계층
- 정규화 계층
- API 제공 계층

### ❌ 선택하지 않은 것

**초저지연 실시간**
- 1분 갱신 주기로 충분 (지표 분석 목적)

**완벽한 데이터 동기화**
- 일시적 지연 허용
- 안정성이 더 중요

**과도한 인프라**
- Redis, Message Queue 등 추가 의존성 제거
- 필요 시 확장 가능하도록 설계

---

## 🚀 실행 방법

### 필수 요구사항

- .NET 9.0 SDK
- (선택) Docker (인프라 확장 시)

### 실행

```bash
# 1. 의존성 복원
dotnet restore

# 2. 서버 실행
dotnet run

# 3. API 테스트
curl "http://localhost:9200/api/v1/summary?symbol=BTCUSDT&interval=1m"
```

### 설정

`DataResource/server_setting.json`:

```json
{
  "host": "localhost",
  "port": 9200,
  "summary_route": "api/v1/summary",
  "pivot_route": "api/v1/pivots",
  "ma_route": "api/v1/moving_averages",
  "oscillator_route": "api/v1/oscillators"
}
```

---

## 🔮 확장 가능성

### Phase 2: 다중 거래소 지원

```csharp
public interface IExchangeKlineManager
{
    Task<bool> Initialize();
    bool GetKlines(string symbol, KlineInterval interval, out List<IKline> klines);
}

// Binance 구현
public class BinanceSocketKlineManager : IExchangeKlineManager { }

// 새로운 거래소 추가
public class UpbitKlineManager : IExchangeKlineManager { }
public class BithumbKlineManager : IExchangeKlineManager { }
```

### Phase 3: 분산 캐시

```
[ WebSocket Receivers ] → [ Redis Pub/Sub ] → [ Multiple API Servers ]
```

### Phase 4: 실시간 알림

```
[ Indicator Threshold ] → [ SignalR WebSocket ] → [ Clients ]
```

---

## 📧 Contact

**GitHub**: [@1985jwlee](https://github.com/1985jwlee)  
**Email**: leejae.w.jl@icloud.com

---

## 📝 License

이 프로젝트는 포트폴리오 목적으로 공개되었습니다.

---

**Last Updated**: 2025-01-22

**Note**: 이 프로젝트는 실제 트레이딩 서비스를 목적으로 하지 않으며, **외부 의존성 격리 및 데이터 플랫폼 설계 능력**을 증명하기 위한 자료입니다.
