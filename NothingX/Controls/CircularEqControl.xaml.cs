using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NothingX.Controls;

public enum EqBandType { Mid, Bass, Treble }

public partial class CircularEqControl : UserControl
{
    public static readonly DependencyProperty BassValueProperty =
        DependencyProperty.Register("BassValue", typeof(float), typeof(CircularEqControl), 
            new FrameworkPropertyMetadata(0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty MidValueProperty =
        DependencyProperty.Register("MidValue", typeof(float), typeof(CircularEqControl), 
            new FrameworkPropertyMetadata(0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty TrebleValueProperty =
        DependencyProperty.Register("TrebleValue", typeof(float), typeof(CircularEqControl), 
            new FrameworkPropertyMetadata(0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public float BassValue
    {
        get => (float)GetValue(BassValueProperty);
        set => SetValue(BassValueProperty, value);
    }

    public float MidValue
    {
        get => (float)GetValue(MidValueProperty);
        set => SetValue(MidValueProperty, value);
    }

    public float TrebleValue
    {
        get => (float)GetValue(TrebleValueProperty);
        set => SetValue(TrebleValueProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircularEqControl control && !control._isDragging)
        {
            control.UpdateUI();
        }
    }

    private bool _isDragging = false;

    public CircularEqControl()
    {
        InitializeComponent();
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawAxes();
        UpdateUI();
    }

    private void DrawAxes()
    {
        AxesCanvas.Children.Clear();
        
        double cx = ActualWidth / 2;
        double cy = ActualHeight / 2;
        double maxRadius = Math.Min(cx, cy) - 20; // 20px padding
        
        if (maxRadius <= 0) return;

        // Draw circles for 0 and max
        var zeroCircle = new Ellipse
        {
            Width = maxRadius,
            Height = maxRadius,
            Stroke = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 4 }
        };
        Canvas.SetLeft(zeroCircle, cx - maxRadius / 2);
        Canvas.SetTop(zeroCircle, cy - maxRadius / 2);
        AxesCanvas.Children.Add(zeroCircle);

        // Draw the 3 axes
        DrawAxis(cx, cy, maxRadius, MidAngle);
        DrawAxis(cx, cy, maxRadius, BassAngle);
        DrawAxis(cx, cy, maxRadius, TrebleAngle);
    }

    private void DrawAxis(double cx, double cy, double length, double angleRads)
    {
        var line = new Line
        {
            X1 = cx,
            Y1 = cy,
            X2 = cx + Math.Cos(angleRads) * length,
            Y2 = cy + Math.Sin(angleRads) * length,
            Stroke = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 2, 4 }
        };
        AxesCanvas.Children.Add(line);
    }

    private const double MidAngle = -Math.PI / 2;
    private const double BassAngle = 150 * Math.PI / 180;
    private const double TrebleAngle = 30 * Math.PI / 180;

    private double GetRadiusForValue(double maxRadius, double value)
    {
        double normalizedValue = (value + 6.0) / 12.0;
        return maxRadius * (0.1 + normalizedValue * 0.9);
    }

    private float GetValueFromRadius(double maxRadius, double radius)
    {
        double normalizedValue = (radius / maxRadius - 0.1) / 0.9;
        normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));
        return (float)Math.Round(-6.0 + (normalizedValue * 12.0));
    }

    private Point GetPointForValue(double cx, double cy, double maxRadius, double value, double angleRads)
    {
        double radius = GetRadiusForValue(maxRadius, value);
        
        return new Point(
            cx + Math.Cos(angleRads) * radius,
            cy + Math.Sin(angleRads) * radius
        );
    }

    private string FormatGain(float value)
    {
        int v = (int)Math.Round(value);
        return v > 0 ? $"+{v}" : v.ToString();
    }

    private void UpdateUI()
    {
        double cx = ActualWidth / 2;
        double cy = ActualHeight / 2;
        double maxRadius = Math.Min(cx, cy) - 20;
        if (maxRadius <= 0) return;

        Point pMid = GetPointForValue(cx, cy, maxRadius, MidValue, MidAngle);
        Point pBass = GetPointForValue(cx, cy, maxRadius, BassValue, BassAngle);
        Point pTreble = GetPointForValue(cx, cy, maxRadius, TrebleValue, TrebleAngle);

        // Update Thumbs
        Canvas.SetLeft(MidThumb, pMid.X);
        Canvas.SetTop(MidThumb, pMid.Y);
        
        Canvas.SetLeft(BassThumb, pBass.X);
        Canvas.SetTop(BassThumb, pBass.Y);
        
        Canvas.SetLeft(TrebleThumb, pTreble.X);
        Canvas.SetTop(TrebleThumb, pTreble.Y);

        // Update Labels (positioned at the outer edge)
        Point lMid = GetPointForValue(cx, cy, maxRadius + 15, 6, MidAngle);
        Canvas.SetLeft(MidLabel, lMid.X - 10);
        Canvas.SetTop(MidLabel, lMid.Y - 15);

        Point lBass = GetPointForValue(cx, cy, maxRadius + 15, 6, BassAngle);
        Canvas.SetLeft(BassLabel, lBass.X - 25);
        Canvas.SetTop(BassLabel, lBass.Y - 5);

        Point lTreble = GetPointForValue(cx, cy, maxRadius + 15, 6, TrebleAngle);
        Canvas.SetLeft(TrebleLabel, lTreble.X + 5);
        Canvas.SetTop(TrebleLabel, lTreble.Y - 5);

        // Update Value Labels (positioned next to each thumb)
        MidValueLabel.Text = FormatGain(MidValue);
        Canvas.SetLeft(MidValueLabel, pMid.X + 12);
        Canvas.SetTop(MidValueLabel, pMid.Y - 8);

        BassValueLabel.Text = FormatGain(BassValue);
        Canvas.SetLeft(BassValueLabel, pBass.X - 28);
        Canvas.SetTop(BassValueLabel, pBass.Y - 4);

        TrebleValueLabel.Text = FormatGain(TrebleValue);
        Canvas.SetLeft(TrebleValueLabel, pTreble.X + 12);
        Canvas.SetTop(TrebleValueLabel, pTreble.Y - 4);

        // Update Blob Polygon
        EqBlob.Points = new PointCollection { pMid, pTreble, pBass };
    }

    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb thumb) return;
        
        _isDragging = true;

        double cx = ActualWidth / 2;
        double cy = ActualHeight / 2;
        double maxRadius = Math.Min(cx, cy) - 20;

        // Current thumb pos
        double currentX = Canvas.GetLeft(thumb) + e.HorizontalChange;
        double currentY = Canvas.GetTop(thumb) + e.VerticalChange;

        // Distance from center
        double dist = Math.Sqrt(Math.Pow(currentX - cx, 2) + Math.Pow(currentY - cy, 2));

        float val = GetValueFromRadius(maxRadius, dist);

        if (Enum.TryParse<EqBandType>(thumb.Tag?.ToString(), out var bandType))
        {
            switch (bandType)
            {
                case EqBandType.Mid:
                    if (MidValue != val) MidValue = val;
                    break;
                case EqBandType.Bass:
                    if (BassValue != val) BassValue = val;
                    break;
                case EqBandType.Treble:
                    if (TrebleValue != val) TrebleValue = val;
                    break;
            }
        }

        UpdateUI();
        _isDragging = false;
    }
}
