using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Davinci.net;
using Quartz;
using Quartz.Impl;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Davinci
{
    public class HelloJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
             Debug.Write("Greetings from HelloJob!");
            await Task.Delay(100);
            await context.Scheduler.DeleteJob(context.JobDetail.Key);
        }
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
             this.Loaded += MainPage_Loaded;
           

        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();
            IScheduler scheduler = await factory.GetScheduler();
            // define the job and tie it to our HelloJob class
            IJobDetail job = JobBuilder.Create<HelloJob>()
                .WithIdentity("job1", "group1")
                .Build();

            // Trigger the job to run now, and then repeat every 10 seconds
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(1)
                    .RepeatForever())
                .Build();
           await scheduler.ScheduleJob(job, trigger);
           await scheduler.Start();
            return;
            var folders = await KnownFolders.MusicLibrary.GetFoldersAsync();

            var dictionary = folders.Select(async f =>
                new KeyValuePair<int, DavinciImage>(int.Parse(f.DisplayName),
                    await DavinciImage.FromTilesAsync((await f.GetFilesAsync())
                        .Select(file => (new Point(double.Parse(file.DisplayName.Split('_').First()),
                            double.Parse(file.DisplayName.Split('_').Last())), file))
                        .ToArray())));

            var results = (await Task.WhenAll(dictionary)).OrderByDescending(r => r.Key);
            await BuildTiles(new int[] { 1, 2, 3, 4 },results);
            await BuildTiles(new int[] { 5,6,7,8 }, results);

            //var orderedTileResults = tilesresult.OrderByDescending(r=>r.Item1);
            //var finaltasks=orderedTileResults.ToList().SelectMany(res=>res.Item2.Select(tuple =>($"{res.Item1}_{tuple.Item1.X}_{tuple.Item1.Y}",tuple.Item2) )).ToList();
            //foreach (var valueTuple in finaltasks)
            //{
            //    valueTuple.Item2.
            //}
            //var imageStorageFile = await KnownFolders.MusicLibrary.GetFileAsync("beautiful.jpg");
            //var imageStorageFile2 = await KnownFolders.MusicLibrary.GetFileAsync("trees.jpg");

            //var davinci = await DavinciImage.FromTilesAsync(
            //    (new Point(1, 1), imageStorageFile),
            //    (new Point(2, 1), imageStorageFile2));

            //GridView.ItemsSource = await
            //    (await davinci.Scale(2).Scale(2).ToTilesAsync(256, 256))
            //    .Select(x => x.Item2.Scale(0.5).ToImageAsync())
            //    .ExecuteAsync();

            //davinci.Dispose();

        }

        private static async Task BuildTiles(int[] ints, IOrderedEnumerable<KeyValuePair<int, DavinciImage>> results)
        {
            var tilesTasks = ints.Select(async index =>
                {
                    var result = results.FirstOrDefault(r => r.Key <= index);
                    var davinciImage = result.Value.Scale((index - result.Key) * 2);
                    var valueTuple = (result.Key, (await davinciImage.ToTilesAsync(256, 256))
                        .Select(async item =>
                        {
                            var storageFile = await item.Item2.ToStorageFileAsync(KnownFolders.MusicLibrary,
                                $"{index}_{item.Item1.X}_{item.Item1.Y}.jpg");
                            item.Item2.Dispose();
                            return (item.Item1, storageFile);
                        })
                        );
                    davinciImage.Dispose();
                    return valueTuple;
                }
            );
            var tasks = await Task.WhenAll(tilesTasks);
            var tilesresult = await Task.WhenAll(tasks.Select(async task => (task.Item1, await Task.WhenAll(task.Item2))));
        }
    }
}
