using RestSharp;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Antivirus.Net
{
    public class VirustotalClient
    {
        public event Action<string> OnRequest;

        private RestClient client;
        private string apikey;

        public VirustotalClient(string url, string apikey)
        {
            this.client = new RestClient(url);
            this.apikey = apikey;
        }

        public IObservable<FileReportResult> GetFileReport(string hash)
        {
            var request = new RestRequest("file/report", Method.GET);
            request.AddQueryParameter("apikey", this.apikey);
            request.AddQueryParameter("resource", hash);

            return this.Execute<FileReportResult>(request);
        }
        public IObservable<FileScanResult> UploadFile(string path)
        {
            var request = new RestRequest("file/scan", Method.POST);
            request.AddQueryParameter("apikey", this.apikey);
            request.AddFile("file", path);

            return this.Execute<FileScanResult>(request);
        }
        public IObservable<CommentResult> GetComments(string hash)
        {
            var request = new RestRequest("comments/get", Method.GET);
            request.AddQueryParameter("apikey", this.apikey);
            request.AddFile("resource", hash);

            return this.Execute<CommentResult>(request);
        }

        private IObservable<T> Execute<T>(RestRequest request)
        {
            return Observable.Defer<T>(() => {
                var subject = new Subject<T>();

                this.OnRequest?.Invoke(request.Resource);
                this.client.ExecuteAsync<T>(request, (response, handle) =>
                {
                    if (response.IsSuccessful)
                    {
                        subject.OnNext(response.Data);
                        subject.OnCompleted();
                    }
                    else subject.OnError(response.ErrorException);
                });
                return subject;
            });
        }
    }
}
