﻿using OpenCvSharp;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Client.ViewModel.Operation
{
    [OperationInfo(5.1, "连通域", MaterialDesignThemes.Wpf.PackIconKind.Connection)]
    public class ConnectedComponentsViewModel : OperaViewModelBase
    {
        [ObservableAsProperty] public int AreaLimit { get; private set; }
        [Reactive] public int AreaMax { get; set; }
        [Reactive] public int AreaMin { get; set; }
        public IList<string> Filters { get; set; }
        [ObservableAsProperty] public int HeightLimit { get; set; }
        [Reactive] public int HeightMax { get; set; }
        [Reactive] public int HeightMin { get; set; }
        [Reactive] public int LeftMax { get; set; }
        [Reactive] public int LeftMin { get; set; }
        [Reactive] public double Point2dDistance { get; set; }
        [Reactive] public double Point2dX { get; set; }
        [Reactive] public double Point2dY { get; set; }
        [Reactive] public int TopMax { get; set; }
        [Reactive] public int TopMin { get; set; }
        [ObservableAsProperty] public int WidthLimit { get; set; }
        [Reactive] public int WidthMax { get; set; }
        [Reactive] public int WidthMin { get; set; }

        private IList<ConnectedComponents.Blob> FilterBlob(IList<ConnectedComponents.Blob> blobs, string filterStr, Mat mat)
        {
            IList<ConnectedComponents.Blob> reBlobs = new List<ConnectedComponents.Blob>();
            switch (filterStr)
            {
                case "Area":
                    reBlobs = blobs.Where(b => b.Area >= AreaMin && b.Area <= AreaMax).ToList();
                    break;

                //case "Centroid":
                //    reBlobs = blobs.Where(b => Point2d.Distance(b.Centroid, new Point2d(Point2dX, Point2dY)) <= Point2dDistance);
                //    break;

                case "Height":
                    reBlobs = blobs.Where(b => b.Height >= HeightMin && b.Height <= HeightMax).ToList();
                    break;

                //case "Label":
                //    reBlobs=blobs.Where(b=>b.la)
                //    break;

                case "Left":
                    reBlobs = blobs.Where(b => b.Left >= LeftMin && b.Left <= LeftMax).ToList();
                    break;

                //case "Rect":
                //    reBlobs=blobs.Where(b=>b.Rect)
                //    break;

                case "Top":
                    reBlobs = blobs.Where(b => b.Top >= TopMin && b.Top <= TopMax).ToList();
                    break;

                case "Width":
                    reBlobs = blobs.Where(b => b.Width >= WidthMin && b.Width <= WidthMax).ToList();
                    break;

                default:
                    break;
            }
            return reBlobs;
        }

        private void UpdateOutput(IList<string> filters = null)
        {
            SendTime(() =>
            {
                ConnectedComponents connCom = _sigleSrc.ConnectedComponentsEx();

                IList<ConnectedComponents.Blob> tmpBlobs1 = connCom.Blobs.ToList();
                IList<ConnectedComponents.Blob> tmpBlobs2;
                if (filters != null)
                {
                    foreach (string filter in filters)
                    {
                        tmpBlobs2 = new List<ConnectedComponents.Blob>(FilterBlob(tmpBlobs1, filter, _sigleSrc));
                        tmpBlobs1 = new List<ConnectedComponents.Blob>(tmpBlobs2.ToList());
                    }
                }
                Mat dst = _rt.NewMat();
                if (tmpBlobs1.Any())
                {
                    //connCom.FilterByBlobs(_sigleSrc, dst, tmpBlobs1);
                    connCom.RenderBlobs(dst, tmpBlobs1, connCom.Labels, connCom.Blobs.Count);
                    _imageDataManager.OutputMatSubject.OnNext(dst.Clone());
                }
                else
                {
                    _imageDataManager.OutputMatSubject.OnNext(_sigleSrc.Clone());
                }
            });
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            IObservable<Guid?> currentMatOb = _imageDataManager.InputMatGuidSubject
                .WhereNotNull()
                .Where(g => CanOperat)
                .ObserveOn(RxApp.MainThreadScheduler);

            currentMatOb
                .Do(g => UpdateOutput(Filters?.ToList()))
                .Subscribe()
                .DisposeWith(d);

            currentMatOb
                .Select(guid => _imageDataManager.GetCurrentMat())
                .WhereNotNull()
                .Select(mat => mat.Width)
                .ToPropertyEx(this, x => x.WidthLimit)
                .DisposeWith(d);

            currentMatOb
                .Select(guid => _imageDataManager.GetCurrentMat())
                .WhereNotNull()
                .Select(mat => mat.Height)
                .ToPropertyEx(this, x => x.HeightLimit)
                .DisposeWith(d);

            currentMatOb
                .Select(guid => _imageDataManager.GetCurrentMat())
                .WhereNotNull()
                .Select(mat => mat.Rows * mat.Cols)
                .ToPropertyEx(this, x => x.AreaLimit)
                .DisposeWith(d);

            IObservable<(int, int)> areaOb = this.WhenAnyValue(x => x.AreaMax, x => x.AreaMin)
                 .Where(vt => Filters != null && Filters.Any() && Filters.Any(t => t.Equals("Area", StringComparison.Ordinal)));
            IObservable<(int, int)> heightOb = this.WhenAnyValue(x => x.HeightMax, x => x.HeightMin)
                 .Where(vt => Filters != null && Filters.Any() && Filters.Any(t => t.Equals("Height", StringComparison.Ordinal)));
            IObservable<(int, int)> widthOb = this.WhenAnyValue(x => x.WidthMax, x => x.WidthMin)
                 .Where(vt => Filters != null && Filters.Any() && Filters.Any(t => t.Equals("Width", StringComparison.Ordinal)));
            IObservable<(int, int)> leftOb = this.WhenAnyValue(x => x.LeftMax, x => x.LeftMin)
                .Where(vt => Filters != null && Filters.Any() && Filters.Any(t => t.Equals("Left", StringComparison.Ordinal)));
            IObservable<(int, int)> topOb = this.WhenAnyValue(x => x.TopMax, x => x.TopMin)
                .Where(vt => Filters != null && Filters.Any() && Filters.Any(t => t.Equals("Top", StringComparison.Ordinal)));

            IObservable<(int, int)> paraOb = Observable.Merge(new[] { areaOb, heightOb, widthOb, leftOb, topOb });
            paraOb
                .Where(b => CanOperat)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .SubscribeOn(RxApp.MainThreadScheduler)
                .Subscribe(b => UpdateOutput(Filters?.ToList()))
                .DisposeWith(d);
        }
    }
}