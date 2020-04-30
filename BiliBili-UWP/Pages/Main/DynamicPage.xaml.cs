﻿using BiliBili_Lib.Models.BiliBili;
using BiliBili_Lib.Service;
using BiliBili_UWP.Models.Core;
using BiliBili_UWP.Models.UI.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BiliBili_UWP.Pages.Main
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DynamicPage : Page, IRefreshPage
    {
        public ObservableCollection<Topic> DynamicCollection = new ObservableCollection<Topic>();
        private List<Topic> TotalList = new List<Topic>();
        private bool _isInit = false;
        private bool _isDynamicRequesting = false;
        private BiliViewModel biliVM = App.BiliViewModel;
        private TopicService _topicService = App.BiliViewModel._client.Topic;
        private string offset = "";
        private double _scrollOffset = 0;
        private bool _isOnlyVideo = false;
        public DynamicPage()
        {
            this.InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            biliVM.IsLoginChanged += IsLoginChanged;
        }

        private async void IsLoginChanged(object sender, bool e)
        {
            await Refresh();
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            App.AppViewModel.CurrentPagePanel.ScrollToBottom = ScrollViewerBottomHandle;
            App.AppViewModel.CurrentPagePanel.ScrollChanged = ScrollViewerChanged;
            if (e.NavigationMode == NavigationMode.Back)
                return;
            if (_isInit)
            {
                GetFollowerUnread();
                return;
            }
            await Refresh();
            _isInit = true;
        }



        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            App.AppViewModel.CurrentPagePanel.ScrollToBottom = null;
            App.AppViewModel.CurrentPagePanel.ScrollChanged = null;
            base.OnNavigatingFrom(e);
        }
        private void Reset()
        {
            DynamicCollection.Clear();
            TotalList.Clear();
            offset = "";
            _scrollOffset = 0;
            HolderText.Visibility = Visibility.Collapsed;
            DynamicLoadingBar.Visibility = Visibility.Collapsed;
        }
        public async Task Refresh()
        {
            Reset();
            await LoadDynamic();
        }

        private async Task LoadDynamic()
        {
            if (_isDynamicRequesting)
                return;
            _isDynamicRequesting = true;
            Tuple<string, List<Topic>> data = null;
            DynamicLoadingBar.Visibility = Visibility.Visible;
            if (string.IsNullOrEmpty(offset))
                data = await _topicService.GetNewDynamicAsync();
            else
                data = await _topicService.GetHistoryDynamicAsync(offset);
            if (data != null)
            {
                offset = data.Item1;
                data.Item2.ForEach(p => TotalList.Add(p));
                DynamicCollectionInit();
            }
            DynamicLoadingBar.Visibility = Visibility.Collapsed;
            HolderText.Visibility = DynamicCollection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            _isDynamicRequesting = false;
        }

        private void DynamicCollectionInit()
        {
            foreach (var item in TotalList)
            {
                if (!DynamicCollection.Contains(item))
                {
                    var temp = DynamicCollection.Where(p => p.desc.timestamp < item.desc.timestamp).FirstOrDefault();
                    int index = 0;
                    if (temp != null)
                        index = DynamicCollection.IndexOf(temp);
                    else
                        index = DynamicCollection.Count;
                    if ((_isOnlyVideo && item.desc.type == 8) || !_isOnlyVideo)
                    {
                        DynamicCollection.Insert(index, item);
                    }
                }
            }
        }

        private async void ScrollViewerBottomHandle()
        {
            await LoadDynamic();
        }
        private void ScrollViewerChanged()
        {
            double offset = App.AppViewModel.CurrentPagePanel.PageScrollViewer.VerticalOffset;
            _scrollOffset = offset;
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_scrollOffset > 0)
            {
                App.AppViewModel.CurrentPagePanel.PageScrollViewer.ChangeView(0, _scrollOffset, 1);
            }
        }
        private async void GetFollowerUnread()
        {
            if (App.BiliViewModel.IsLogin)
            {
                var count = await App.BiliViewModel._client.GetFollowerUnreadCountAsync();
                if (count > 0)
                {
                    //刷新动态
                    if (_isDynamicRequesting)
                        return;
                    _isDynamicRequesting = true;
                    var data = await _topicService.GetNewDynamicAsync();
                    if (data != null)
                    {
                        for (int i = data.Item2.Count - 1; i >= 0; i--)
                        {
                            if (!TotalList.Contains(data.Item2[i]))
                                TotalList.Insert(0, data.Item2[i]);
                        }
                        DynamicCollectionInit();
                    }
                }
            }
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isInit)
                return;
            _isOnlyVideo = (sender as ToggleSwitch).IsOn;
            if (_isOnlyVideo)
            {
                for (int i = DynamicCollection.Count - 1; i >= 0; i--)
                {
                    if (DynamicCollection[i].desc.type != 8)
                        DynamicCollection.RemoveAt(i);
                }
            }
            else
            {
                DynamicCollectionInit();
            }
        }
    }
}
