using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Profiling; // ProfilerMarker 사용을 위한 네임스페이스
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

public class AsyncPerformanceBenchmark : MonoBehaviour
{
    [SerializeField] private int iterationCount = 100;

    // 프로파일러 검색창에서 "[Benchmark]" 입력 시 쉽게 검색할 수 있도록 static 마커 정의
    private static readonly ProfilerMarker TaskInvokeMarker = new ProfilerMarker("[Benchmark] Task.Invoke");
    private static readonly ProfilerMarker TaskExecMarker = new ProfilerMarker("[Benchmark] Task.Execute");

    private static readonly ProfilerMarker UniTaskInvokeMarker = new ProfilerMarker("[Benchmark] UniTask.Invoke");
    private static readonly ProfilerMarker UniTaskExecMarker = new ProfilerMarker("[Benchmark] UniTask.Execute");

    private async void Start()
    {
        // JIT 워밍업
        await Warmup();

        Debug.Log($"=== 성능 측정 시작 (반복 횟수: {iterationCount:N0}회) ===");

        // 1. System.Threading.Tasks.Task 측정
        await MeasureTask();

        // 2. UniTask 측정
        await MeasureUniTask();

        Debug.Log("=== 성능 측정 완료 ===");
    }

    private async UniTask Warmup()
    {
        await Task.Yield();
        await UniTask.Yield();
    }

    private async UniTask MeasureTask()
    {
        PrepareGC();
        long startMemory = GC.GetTotalMemory(true);

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < iterationCount; i++)
        {
            // 1) 비동기 호출 및 Task 객체 생성 구간 마킹
            TaskInvokeMarker.Begin();
            Task<int> task = ExecuteTaskAsync(i);
            TaskInvokeMarker.End();

            // 2) 프레임 대기 및 완료 구간
            await task;
        }

        sw.Stop();

        long endMemory = GC.GetTotalMemory(false);
        long allocatedMemory = Math.Max(0, endMemory - startMemory);

        Debug.Log($"[Task] 소요 시간: {sw.ElapsedMilliseconds} ms | GC 할당량 추정: {allocatedMemory / 1024f:F2} KB");
    }

    private async UniTask MeasureUniTask()
    {
        PrepareGC();
        long startMemory = GC.GetTotalMemory(true);

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < iterationCount; i++)
        {
            // 1) 비동기 호출 및 UniTask 생성 구간 마킹
            UniTaskInvokeMarker.Begin();
            UniTask<int> task = ExecuteUniTaskAsync(i);
            UniTaskInvokeMarker.End();

            // 2) 프레임 대기 및 완료 구간
            await task;
        }

        sw.Stop();

        long endMemory = GC.GetTotalMemory(false);
        long allocatedMemory = Math.Max(0, endMemory - startMemory);

        Debug.Log($"[UniTask] 소요 시간: {sw.ElapsedMilliseconds} ms | GC 할당량 추정: {allocatedMemory / 1024f:F2} KB");
    }

    // -------------------------------------------------------------
    // 테스트 대상 비동기 메서드
    // -------------------------------------------------------------

    private async Task<int> ExecuteTaskAsync(int value)
    {
        TaskExecMarker.Begin();
        int result = value + 1;
        TaskExecMarker.End();

        await Task.Yield();
        return result;
    }

    private async UniTask<int> ExecuteUniTaskAsync(int value)
    {
        UniTaskExecMarker.Begin();
        int result = value + 1;
        UniTaskExecMarker.End();

        await UniTask.Yield();
        return result;
    }

    private void PrepareGC()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}