using Foundation;
using SkiaSharp.Views.Maui.Controls;
using UIKit;
using CoreGraphics;

namespace BRM_2.Platforms.MacCatalyst;

/// <summary>
/// Mac Catalyst handler for SKCanvasView that enables scroll wheel support
/// </summary>
public class SKCanvasViewHandler : SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler
{
    protected override SkiaSharp.Views.iOS.SKCanvasView CreatePlatformView()
    {
        var platformView = base.CreatePlatformView();

        // Add scroll gesture recognizer to handle scroll wheel on Mac Catalyst
        var scrollRecognizer = new UIScrollViewScrollWheelGestureRecognizer();
        platformView.AddGestureRecognizer(scrollRecognizer);

        return platformView;
    }
}

/// <summary>
/// Custom gesture recognizer for scroll wheel events on Mac Catalyst
/// </summary>
public class UIScrollViewScrollWheelGestureRecognizer : UIGestureRecognizer
{
    public UIScrollViewScrollWheelGestureRecognizer() : base()
    {
    }

    public override void TouchesBegan(NSSet touches, UIEvent evt)
    {
        base.TouchesBegan(touches, evt);
        CheckForScrollWheel(evt);
    }

    public override void TouchesMoved(NSSet touches, UIEvent evt)
    {
        base.TouchesMoved(touches, evt);
        CheckForScrollWheel(evt);
    }

    public override void TouchesEnded(NSSet touches, UIEvent evt)
    {
        base.TouchesEnded(touches, evt);
        State = UIGestureRecognizerState.Cancelled;
    }

    public override void TouchesCancelled(NSSet touches, UIEvent evt)
    {
        base.TouchesCancelled(touches, evt);
        State = UIGestureRecognizerState.Cancelled;
    }

    private void CheckForScrollWheel(UIEvent evt)
    {
        try
        {
            // Check if this is a scroll wheel event by examining the event type
            // On Mac Catalyst, scroll wheel events have UIEventType.Scroll
            if (evt?.Type == UIEventType.Scroll)
            {
                State = UIGestureRecognizerState.Recognized;
                HandleScrollWheelEvent(evt);
                State = UIGestureRecognizerState.Cancelled;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CheckForScrollWheel error: {ex.Message}");
        }
    }

    private void HandleScrollWheelEvent(UIEvent uiEvent)
    {
        try
        {
            // Try to extract scroll delta information from the event using reflection
            if (TryExtractScrollDelta(uiEvent, out double deltaX, out double deltaY))
            {
                System.Diagnostics.Debug.WriteLine($"Scroll wheel detected: DeltaX={deltaX}, DeltaY={deltaY}");

                // Post notification that can be handled by SpectrogramView
                NSNotificationCenter.DefaultCenter.PostNotificationName(
                    new NSString("ScrollWheelEventReceived"),
                    this,
                    NSDictionary.FromObjectsAndKeys(
                        new object[] { deltaX, deltaY },
                        new object[] { "deltaX", "deltaY" }
                    )
                );
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HandleScrollWheelEvent error: {ex.Message}");
        }
    }

    private bool TryExtractScrollDelta(UIEvent uiEvent, out double deltaX, out double deltaY)
    {
        deltaX = 0;
        deltaY = 0;

        try
        {
            var eventType = uiEvent.GetType();

            // Try to get scroll values through reflection
            // The property names may vary depending on the iOS/Mac Catalyst version
            var properties = eventType.GetProperties();

            foreach (var prop in properties)
            {
                if (prop.Name.Contains("Scroll", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var value = prop.GetValue(uiEvent);
                        if (value is double dValue)
                        {
                            if (prop.Name.Contains("ScrollX", StringComparison.OrdinalIgnoreCase))
                                deltaX = dValue;
                            else if (prop.Name.Contains("ScrollY", StringComparison.OrdinalIgnoreCase))
                                deltaY = dValue;
                        }
                    }
                    catch { }
                }
            }

            return deltaX != 0 || deltaY != 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TryExtractScrollDelta error: {ex.Message}");
            return false;
        }
    }
}
