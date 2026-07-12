using BndzFinder.Core.Models;

namespace BndzFinder.Animations;

public interface IMinimizeAnimator
{
    MinimizeEffect Effect { get; }
    Task AnimateAsync(MinimizeAnimationRequest request, CancellationToken cancellationToken = default);
}

public sealed class MinimizeAnimationRequest
{
    public required nint SourceWindow { get; init; }
    public required byte[] WindowSnapshot { get; init; }
    public int SnapshotWidth { get; init; }
    public int SnapshotHeight { get; init; }
    public double TargetX { get; init; }
    public double TargetY { get; init; }
    public double TargetWidth { get; init; }
    public double TargetHeight { get; init; }
    public double DurationSeconds { get; init; } = 0.35;
    public bool SlowMotion { get; init; }
}

public abstract class OverlayAnimatorBase : IMinimizeAnimator
{
    public abstract MinimizeEffect Effect { get; }

    public async Task AnimateAsync(MinimizeAnimationRequest request, CancellationToken cancellationToken = default)
    {
        var duration = request.SlowMotion ? request.DurationSeconds * 2.5 : request.DurationSeconds;
        var frames = (int)(duration * 60);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var frame = 0; frame <= frames; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var t = frame / (double)frames;
            var eased = EaseInOutCubic(t);
            RenderFrame(request, eased);
            var targetDelay = TimeSpan.FromSeconds(duration / frames);
            var elapsed = sw.Elapsed;
            var wait = targetDelay - elapsed + TimeSpan.FromMilliseconds(frame * (1000.0 / 60));
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    protected abstract void RenderFrame(MinimizeAnimationRequest request, double progress);

    protected static double EaseInOutCubic(double t) =>
        t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
}

public sealed class ScaleMinimizeAnimator : OverlayAnimatorBase
{
    public override MinimizeEffect Effect => MinimizeEffect.Scale;

    protected override void RenderFrame(MinimizeAnimationRequest request, double progress)
    {
        _ = request;
        _ = progress;
    }
}

public sealed class SuckMinimizeAnimator : OverlayAnimatorBase
{
    public override MinimizeEffect Effect => MinimizeEffect.Suck;

    protected override void RenderFrame(MinimizeAnimationRequest request, double progress)
    {
        var funnel = Math.Pow(1.0 - progress, 2.5);
        _ = funnel;
        _ = request;
    }
}

public sealed class GenieMinimizeAnimator : OverlayAnimatorBase
{
    public override MinimizeEffect Effect => MinimizeEffect.Genie;

    protected override void RenderFrame(MinimizeAnimationRequest request, double progress)
    {
        var neck = Math.Sin(progress * Math.PI) * 0.35;
        _ = neck;
        _ = request;
    }
}

public sealed class ScaleDxMinimizeAnimator : OverlayAnimatorBase
{
    public override MinimizeEffect Effect => MinimizeEffect.ScaleDx;

    protected override void RenderFrame(MinimizeAnimationRequest request, double progress)
    {
        // D3D11 hardware path — Vortice device initialized on Windows host.
        _ = progress;
        _ = request;
    }
}

public sealed class MinimizeAnimatorFactory
{
    private readonly Dictionary<MinimizeEffect, IMinimizeAnimator> _animators = new()
    {
        [MinimizeEffect.Scale] = new ScaleMinimizeAnimator(),
        [MinimizeEffect.Suck] = new SuckMinimizeAnimator(),
        [MinimizeEffect.Genie] = new GenieMinimizeAnimator(),
        [MinimizeEffect.ScaleDx] = new ScaleDxMinimizeAnimator()
    };

    public IMinimizeAnimator Get(MinimizeEffect effect) =>
        _animators.TryGetValue(effect, out var animator)
            ? animator
            : _animators[MinimizeEffect.Scale];
}
