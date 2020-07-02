using CodeArts;
using CodeArts.Net;
using CodeArts.Serialize.Json;
using CodeArts.Serialize.Xml;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace System
{
    /// <summary>
    /// 地址拓展
    /// </summary>
    public static class UriExtentions
    {
        private static readonly Regex UriPattern = new Regex(@"\w+://.+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private abstract class Requestable<T> : IRequestable<T>
        {
            public T Get(int timeout = 5000) => Request("GET", timeout);
            public T Delete(int timeout = 5000) => Request("DELETE", timeout);
            public T Post(int timeout = 5000) => Request("POST", timeout);
            public T Put(int timeout = 5000) => Request("PUT", timeout);
            public T Head(int timeout = 5000) => Request("HEAD", timeout);
            public T Patch(int timeout = 5000) => Request("PATCH", timeout);
            public T Request(string method, int timeout = 5000)
            {
                try
                {
                    return RequestImplement(method, timeout);
                }
                finally
                {
                    Dispose();
                }
            }
            public abstract T RequestImplement(string method, int timeout = 5000);

#if !NET40
            public Task<T> GetAsync(int timeout = 5000) => RequestAsync("GET", timeout);


            public Task<T> DeleteAsync(int timeout = 5000) => RequestAsync("DELETE", timeout);


            public Task<T> PostAsync(int timeout = 5000) => RequestAsync("POST", timeout);


            public Task<T> PutAsync(int timeout = 5000) => RequestAsync("PUT", timeout);


            public Task<T> HeadAsync(int timeout = 5000) => RequestAsync("HEAD", timeout);


            public Task<T> PatchAsync(int timeout = 5000) => RequestAsync("PATCH", timeout);

            public async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await RequestImplementAsync(method, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            public abstract Task<T> RequestImplementAsync(string method, int timeout = 5000);
#endif

            #region IDisposable Support
            private bool disposedValue = false; // 要检测冗余调用

            /// <inheritdoc />
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if (!disposedValue)
                {
                    // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
                    Dispose(true);

                    disposedValue = true;
                }
            }
            #endregion
        }

        private interface IDisposableFileRequestable : IDisposable
        {
            /// <summary>
            /// 上传文件
            /// </summary>
            /// <param name="fileName">本地文件地址：指定文件将被上传到服务地址。</param>
            /// <param name="timeout">超时时间，单位：毫秒</param>
            byte[] UploadFile(string fileName, int timeout = 5000);

            /// <summary>
            /// 上传文件
            /// </summary>
            /// <param name="method">求取方式</param>
            /// <param name="fileName">本地文件地址：指定文件将被上传到服务地址。</param>
            /// <param name="timeout">超时时间，单位：毫秒</param>
            byte[] UploadFile(string method, string fileName, int timeout = 5000);

            /// <summary>
            /// 下载文件
            /// </summary>
            /// <param name="fileName">本地文件地址：文件将下载到这个路径下。</param>
            /// <param name="timeout">超时时间，单位：毫秒</param>
            void DownloadFile(string fileName, int timeout = 5000);

#if !NET40
            /// <summary>
            /// 上传文件
            /// </summary>
            /// <param name="fileName">本地文件地址：指定文件将被上传到服务地址。</param>
            /// <param name="timeout">超时时间，单位：毫秒</param>
            Task<byte[]> UploadFileAsync(string fileName, int timeout = 5000);

            /// <summary>
            /// 上传文件
            /// </summary>
            /// <param name="method">求取方式</param>
            /// <param name="fileName">本地文件地址：指定文件将被上传到服务地址。</param>
            /// <param name="timeout">超时时间，单位：毫秒</param>
            Task<byte[]> UploadFileAsync(string method, string fileName, int timeout = 5000);

            /// <summary>
            /// 下载文件
            /// </summary>
            /// <param name="fileName">本地文件地址：文件将下载到这个路径下。</param>
            /// <param name="timeout">超时时间，单位：毫秒</param>
            Task DownloadFileAsync(string fileName, int timeout = 5000);
#endif
        }

        private class VerifyRequestableExtend<T> : Requestable<T>, IVerifyRequestableExtend<T>, IResendVerifyRequestableExtend<T>, IResendIntervalVerifyRequestableExtend<T>
        {
            private int maxRetires = 1;
            private int millisecondsTimeout = -1;
            private Func<T, int, int> interval;

            private Requestable<T> requestable;
            private List<Predicate<T>> predicates;

            public VerifyRequestableExtend(Requestable<T> requestable, Predicate<T> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                predicates = new List<Predicate<T>> { predicate };

                this.requestable = requestable;
            }

            public IVerifyRequestableExtend<T> And(Predicate<T> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                predicates.Add(predicate);

                return this;
            }

            public override T RequestImplement(string method, int timeout = 5000)
            {
                int times = 0;
                do
                {
                    T result = requestable.RequestImplement(method, timeout);

                    if (predicates.All(x => x.Invoke(result)))
                    {
                        return result;
                    }

                    if (++times <= maxRetires)
                    {
                        if (interval != null)
                        {
                            millisecondsTimeout = interval.Invoke(result, times);
                        }

                        if (millisecondsTimeout > -1)
                        {
                            Thread.Sleep(millisecondsTimeout);
                        }

                        continue;
                    }

                    return result;

                } while (true);
            }
#if !NET40
            public override async Task<T> RequestImplementAsync(string method, int timeout = 5000)
            {
                int times = 0;

                do
                {
                    T result = await requestable.RequestImplementAsync(method, timeout);

                    if (predicates.All(x => x.Invoke(result)))
                    {
                        return result;
                    }

                    if (++times <= maxRetires)
                    {
                        if (interval != null)
                        {
                            millisecondsTimeout = interval.Invoke(result, times);
                        }

                        if (millisecondsTimeout > -1)
                        {
                            await Task.Delay(millisecondsTimeout);
                        }

                        continue;
                    }

                    return result;

                } while (true);
            }
#endif

            public IResendVerifyRequestableExtend<T> ResendCount(int retryCount)
            {
                if (retryCount < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(retryCount), "重试次数必须大于零。");
                }

                maxRetires = Math.Max(retryCount, maxRetires);

                return this;
            }

            public IResendIntervalVerifyRequestableExtend<T> ResendInterval(int millisecondsTimeout)
            {
                if (millisecondsTimeout < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), "重试时间间隔不能小于零。");
                }

                this.millisecondsTimeout = millisecondsTimeout;

                return this;
            }

            public IResendIntervalVerifyRequestableExtend<T> ResendInterval(Func<T, int, int> interval)
            {
                if (interval is null)
                {
                    throw new ArgumentNullException(nameof(interval));
                }

                this.interval = interval;

                return this;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();

                    requestable = null;
                    predicates = null;
                    interval = null;
                }

                base.Dispose(disposing);
            }
        }

        private abstract class RequestableExtend<T> : Requestable<T>, IRequestableExtend<T>
        {
            public IVerifyRequestableExtend<T> DataVerify(Predicate<T> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                return new VerifyRequestableExtend<T>(this, predicate);
            }
        }

        #region 补充
        private class CatchRequestable : RequestableExtend<string>, ICatchRequestable, IResultStringCatchRequestable
        {
            private Action<WebException> log;
            private Func<WebException, string> returnValue;
            private Requestable<string> requestable;
            private IDisposableFileRequestable file;

            public CatchRequestable(Requestable<string> requestable, IDisposableFileRequestable file, Action<WebException> log)
            {
                this.requestable = requestable;
                this.file = file;
                this.log = log ?? throw new ArgumentNullException(nameof(log));
            }

            public ICatchRequestable WebCatch(Action<WebException> log)
            {
                if (log is null)
                {
                    throw new ArgumentNullException(nameof(log));
                }

                this.log += log;

                return this;
            }

            public IResultStringCatchRequestable WebCatch(Func<WebException, string> returnValue)
            {
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));

                return this;
            }

            public IFinallyRequestable Finally(Action log) => new FinallyRequestable(this, file, log);

            IFinallyStringRequestable IResultStringCatchRequestable.Finally(Action log) => new FinallyRequestable(this, file, log);

            public IJsonRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonRequestable<T>(this, namingType);

            public IJsonRequestable<T> JsonCast<T>(T _, NamingType namingType = NamingType.Normal) where T : class => JsonCast<T>(namingType);

            public override string RequestImplement(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.RequestImplement(method, timeout);
                }
                catch (WebException e)
                {
                    log.Invoke(e);

                    if (returnValue is null)
                    {
                        throw;
                    }

                    return returnValue.Invoke(e);
                }
            }

            public byte[] UploadFile(string fileName, int timeout = 5000) => UploadFile(null, fileName, timeout);

            public byte[] UploadFile(string method, string fileName, int timeout = 5000)
            {
                try
                {
                    return file.UploadFile(method, fileName, timeout);
                }
                catch (WebException e)
                {
                    log.Invoke(e);

                    throw;
                }
            }

            public void DownloadFile(string fileName, int timeout = 5000)
            {
                try
                {
                    file.DownloadFile(fileName, timeout);
                }
                catch (WebException e)
                {
                    log.Invoke(e);

                    throw;
                }
            }

            byte[] IFileRequestable.UploadFile(string fileName, int timeout)
            {
                try
                {
                    return UploadFile(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            byte[] IFileRequestable.UploadFile(string method, string fileName, int timeout)
            {
                try
                {
                    return UploadFile(method, fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            void IFileRequestable.DownloadFile(string fileName, int timeout)
            {
                try
                {
                    DownloadFile(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }
#if !NET40
            public override async Task<string> RequestImplementAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestImplementAsync(method, timeout);
                }
                catch (WebException e)
                {
                    log.Invoke(e);

                    if (returnValue is null)
                    {
                        throw;
                    }

                    return returnValue.Invoke(e);
                }
            }

            public async Task<byte[]> UploadFileAsync(string fileName, int timeout = 5000) => await UploadFileAsync(null, fileName, timeout);

            public async Task<byte[]> UploadFileAsync(string method, string fileName, int timeout = 5000)
            {
                try
                {
                    return await file.UploadFileAsync(method, fileName, timeout);
                }
                catch (WebException e)
                {
                    log.Invoke(e);

                    throw;
                }
            }

            public async Task DownloadFileAsync(string fileName, int timeout = 5000)
            {
                try
                {
                    await file.DownloadFileAsync(fileName, timeout);
                }
                catch (WebException e)
                {
                    log.Invoke(e);

                    throw;
                }
            }

            async Task<byte[]> IFileRequestable.UploadFileAsync(string fileName, int timeout)
            {
                try
                {
                    return await UploadFileAsync(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            async Task<byte[]> IFileRequestable.UploadFileAsync(string method, string fileName, int timeout)
            {
                try
                {
                    return await UploadFileAsync(method, fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            async Task IFileRequestable.DownloadFileAsync(string fileName, int timeout)
            {
                try
                {
                    await DownloadFileAsync(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

#endif

            public IXmlRequestable<T> XmlCast<T>() where T : class => new XmlRequestable<T>(this);

            public IXmlRequestable<T> XmlCast<T>(T _) where T : class => XmlCast<T>();


            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();
                    file?.Dispose();
                    log = null;
                    file = null;
                    requestable = null;
                    returnValue = null;
                }

                base.Dispose(disposing);
            }
        }

        private class FinallyRequestable : RequestableExtend<string>, IFinallyRequestable, IFinallyStringRequestable
        {
            private Action log;
            private Requestable<string> requestable;
            private IDisposableFileRequestable file;

            public FinallyRequestable(Requestable<string> requestable, IDisposableFileRequestable file, Action log)
            {
                this.requestable = requestable;
                this.file = file;
                this.log = log ?? throw new ArgumentNullException(nameof(log));
            }

            public IJsonRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonRequestable<T>(this, namingType);

            public IJsonRequestable<T> JsonCast<T>(T _, NamingType namingType = NamingType.Normal) where T : class => JsonCast<T>(namingType);

            public override string RequestImplement(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.RequestImplement(method, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }

            public byte[] UploadFile(string fileName, int timeout = 5000) => UploadFile(null, fileName, timeout);

            public byte[] UploadFile(string method, string fileName, int timeout = 5000)
            {
                try
                {
                    return file.UploadFile(method, fileName, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }

            public void DownloadFile(string fileName, int timeout = 5000)
            {
                try
                {
                    file.DownloadFile(fileName, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }

            byte[] IFileRequestable.UploadFile(string fileName, int timeout)
            {
                try
                {
                    return UploadFile(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            byte[] IFileRequestable.UploadFile(string method, string fileName, int timeout)
            {
                try
                {
                    return UploadFile(method, fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            void IFileRequestable.DownloadFile(string fileName, int timeout)
            {
                try
                {
                    DownloadFile(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

#if !NET40
            public override async Task<string> RequestImplementAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestImplementAsync(method, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }

            public async Task<byte[]> UploadFileAsync(string fileName, int timeout = 5000) => await UploadFileAsync(null, fileName, timeout);

            public async Task<byte[]> UploadFileAsync(string method, string fileName, int timeout = 5000)
            {
                try
                {
                    return await file.UploadFileAsync(method, fileName, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }

            public async Task DownloadFileAsync(string fileName, int timeout = 5000)
            {
                try
                {
                    await file.DownloadFileAsync(fileName, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }

            async Task<byte[]> IFileRequestable.UploadFileAsync(string fileName, int timeout)
            {
                try
                {
                    return await UploadFileAsync(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            async Task<byte[]> IFileRequestable.UploadFileAsync(string method, string fileName, int timeout)
            {
                try
                {
                    return await UploadFileAsync(method, fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            async Task IFileRequestable.DownloadFileAsync(string fileName, int timeout)
            {
                try
                {
                    await DownloadFileAsync(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }
#endif

            public IXmlRequestable<T> XmlCast<T>() where T : class => new XmlRequestable<T>(this);

            public IXmlRequestable<T> XmlCast<T>(T anonymousTypeObject) where T : class => XmlCast<T>();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();
                    file?.Dispose();
                    log = null;
                    file = null;
                    requestable = null;
                }

                base.Dispose(disposing);
            }
        }

        private class IIFThenRequestable : RequestableExtend<string>, IThenRequestable, IRetryThenRequestable, IRetryIntervalThenRequestable
        {
            private int maxRetires = 1;
            private int millisecondsTimeout = -1;
            private Func<WebException, int, int> interval;

            private Requestable<string> requestable;
            private IDisposableFileRequestable file;
            private List<Predicate<WebException>> predicates;

            public IIFThenRequestable(Requestable<string> requestable, IDisposableFileRequestable file, Predicate<WebException> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                this.requestable = requestable;
                this.file = file;
                predicates = new List<Predicate<WebException>> { predicate };
            }

            public IIFThenRequestable(Requestable requestable, Predicate<WebException> predicate) : this(requestable, requestable, predicate)
            {
            }

            public IFinallyRequestable Finally(Action log) => new FinallyRequestable(this, file, log);

            public IThenRequestable Or(Predicate<WebException> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                predicates.Add(predicate);

                return this;
            }

            public IRetryThenRequestable RetryCount(int retryCount)
            {
                if (retryCount < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(retryCount), "重试次数必须大于零。");
                }

                maxRetires = Math.Max(retryCount, maxRetires);

                return this;
            }

            public IRetryIntervalThenRequestable RetryInterval(int millisecondsTimeout)
            {
                if (millisecondsTimeout < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), "重试时间间隔不能小于零。");
                }

                this.millisecondsTimeout = millisecondsTimeout;

                return this;
            }

            public IRetryIntervalThenRequestable RetryInterval(Func<WebException, int, int> interval)
            {
                this.interval = interval ?? throw new ArgumentNullException(nameof(interval));

                return this;
            }

            private void IntervalSleep(WebException exception, int times)
            {
                if (interval != null)
                {
                    millisecondsTimeout = interval.Invoke(exception, times);
                }

                if (millisecondsTimeout > -1)
                {
                    Thread.Sleep(millisecondsTimeout);
                }
            }

            public override string RequestImplement(string method, int timeout = 5000)
            {
                int times = 0;
                do
                {
                    try
                    {
                        return requestable.RequestImplement(method, timeout);
                    }
                    catch (WebException e)
                    {
                        if (times < maxRetires && predicates.Any(x => x.Invoke(e)))
                        {
                            IntervalSleep(e, ++times);

                            continue;
                        }

                        throw;
                    }

                } while (true);
            }

            public ICatchRequestable WebCatch(Action<WebException> log) => new CatchRequestable(this, file, log);

            public IResultStringCatchRequestable WebCatch(Func<WebException, string> returnValue) => new ResultStringCatchRequestable(this, returnValue);

            public IJsonRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonRequestable<T>(this, namingType);

            public IXmlRequestable<T> XmlCast<T>() where T : class => new XmlRequestable<T>(this);

            public IJsonRequestable<T> JsonCast<T>(T _, NamingType namingType = NamingType.Normal) where T : class => JsonCast<T>(namingType);

            public IXmlRequestable<T> XmlCast<T>(T _) where T : class => XmlCast<T>();

            public byte[] UploadFile(string fileName, int timeout = 5000) => UploadFile(null, fileName, timeout);

            public byte[] UploadFile(string method, string fileName, int timeout = 5000)
            {
                int times = 0;
                do
                {
                    try
                    {
                        return file.UploadFile(method, fileName, timeout);
                    }
                    catch (WebException e)
                    {
                        if (times < maxRetires && predicates.Any(x => x.Invoke(e)))
                        {
                            IntervalSleep(e, ++times);

                            continue;
                        }

                        throw;
                    }

                } while (true);
            }

            public void DownloadFile(string fileName, int timeout = 5000)
            {
                int times = 0;

                do
                {
                    try
                    {
                        file.DownloadFile(fileName, timeout);

                        break;
                    }
                    catch (WebException e)
                    {
                        if (times < maxRetires && predicates.Any(x => x.Invoke(e)))
                        {
                            IntervalSleep(e, ++times);

                            continue;
                        }

                        throw;
                    }

                } while (true);
            }

            byte[] IFileRequestable.UploadFile(string fileName, int timeout)
            {
                try
                {
                    return UploadFile(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            byte[] IFileRequestable.UploadFile(string method, string fileName, int timeout)
            {
                try
                {
                    return UploadFile(method, fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            void IFileRequestable.DownloadFile(string fileName, int timeout)
            {
                try
                {
                    DownloadFile(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

#if !NET40
            public override async Task<string> RequestImplementAsync(string method, int timeout = 5000)
            {
                int times = 0;

                do
                {
                    try
                    {
                        return await requestable.RequestImplementAsync(method, timeout);
                    }
                    catch (WebException e)
                    {
                        if (++times <= maxRetires && predicates.Any(x => x.Invoke(e)))
                        {
                            if (interval != null)
                            {
                                millisecondsTimeout = interval.Invoke(e, times);
                            }

                            if (millisecondsTimeout > -1)
                            {
                                await Task.Delay(millisecondsTimeout);
                            }

                            continue;
                        }

                        throw;
                    }

                } while (true);
            }

            public async Task<byte[]> UploadFileAsync(string fileName, int timeout = 5000) => await UploadFileAsync(null, fileName, timeout);

            public async Task<byte[]> UploadFileAsync(string method, string fileName, int timeout = 5000)
            {
                int times = 0;

                do
                {
                    try
                    {
                        return await file.UploadFileAsync(method, fileName, timeout);
                    }
                    catch (WebException e)
                    {
                        if (++times <= maxRetires && predicates.Any(x => x.Invoke(e)))
                        {
                            if (interval != null)
                            {
                                millisecondsTimeout = interval.Invoke(e, times);
                            }

                            if (millisecondsTimeout > -1)
                            {
                                await Task.Delay(millisecondsTimeout);
                            }

                            continue;
                        }

                        throw;
                    }

                } while (true);
            }

            public async Task DownloadFileAsync(string fileName, int timeout = 5000)
            {
                int times = 0;

                do
                {
                    try
                    {
                        await file.DownloadFileAsync(fileName, timeout);

                        break;
                    }
                    catch (WebException e)
                    {
                        if (++times <= maxRetires && predicates.Any(x => x.Invoke(e)))
                        {
                            if (interval != null)
                            {
                                millisecondsTimeout = interval.Invoke(e, times);
                            }

                            if (millisecondsTimeout > -1)
                            {
                                await Task.Delay(millisecondsTimeout);
                            }

                            continue;
                        }

                        throw;
                    }

                } while (true);
            }

            async Task<byte[]> IFileRequestable.UploadFileAsync(string fileName, int timeout)
            {
                try
                {
                    return await UploadFileAsync(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            async Task<byte[]> IFileRequestable.UploadFileAsync(string method, string fileName, int timeout)
            {
                try
                {
                    return await UploadFileAsync(method, fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            async Task IFileRequestable.DownloadFileAsync(string fileName, int timeout)
            {
                try
                {
                    await DownloadFileAsync(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }
#endif

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();
                    file?.Dispose();
                    requestable = null;
                    predicates = null;
                    interval = null;
                    file = null;
                }

                base.Dispose(disposing);
            }
        }

        private class ThenRequestable : RequestableExtend<string>, IThenConditionRequestable, IThenAndConditionRequestable, IDisposableFileRequestable
        {
            private volatile bool isAllocated = false;

            private IRequestableBase requestable;
            private Requestable<string> request;
            private IDisposableFileRequestable file;
            private List<Predicate<WebException>> predicates;
            private Action<IRequestableBase, WebException> then;

            private ThenRequestable(Requestable<string> request, IDisposableFileRequestable file, IRequestableBase requestable, Action<IRequestableBase, WebException> then)
            {
                this.request = request;
                this.requestable = requestable;
                this.file = file;
                this.then = then ?? throw new ArgumentNullException(nameof(then));
                predicates = new List<Predicate<WebException>>();
            }

            public ThenRequestable(Requestable requestable, Action<IRequestableBase, WebException> then) : this(requestable, requestable, requestable, then)
            {

            }

            public IFinallyRequestable Finally(Action log) => new FinallyRequestable(this, file, log);

            public IThenRequestable TryIf(Predicate<WebException> match) => new IIFThenRequestable(this, this, match);

            public IThenConditionRequestable Then(Action<IRequestableBase, WebException> then) => new ThenRequestable(this, this, requestable, then);

            public override string RequestImplement(string method, int timeout = 5000)
            {
                if (isAllocated)
                {
                    return request.RequestImplement(method, timeout);
                }

                try
                {
                    return request.RequestImplement(method, timeout);
                }
                catch (WebException e)
                {
                    if (predicates.All(x => x.Invoke(e)))
                    {
                        isAllocated = true;

                        then.Invoke(requestable, e);

                        return request.RequestImplement(method, timeout);
                    }

                    throw;
                }
            }

            public ICatchRequestable WebCatch(Action<WebException> log) => new CatchRequestable(this, file, log);

            public IResultStringCatchRequestable WebCatch(Func<WebException, string> returnValue) => new ResultStringCatchRequestable(this, returnValue);

            public IJsonRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonRequestable<T>(this, namingType);

            public IXmlRequestable<T> XmlCast<T>() where T : class => new XmlRequestable<T>(this);

            public IJsonRequestable<T> JsonCast<T>(T _, NamingType namingType = NamingType.Normal) where T : class => JsonCast<T>(namingType);

            public IXmlRequestable<T> XmlCast<T>(T _) where T : class => XmlCast<T>();

            public byte[] UploadFile(string fileName, int timeout = 5000) => UploadFile(null, fileName, timeout);

            public byte[] UploadFile(string method, string fileName, int timeout = 5000)
            {
                if (isAllocated)
                {
                    return file.UploadFile(method, fileName, timeout);
                }

                try
                {
                    return file.UploadFile(method, fileName, timeout);
                }
                catch (WebException e)
                {
                    if (predicates.All(x => x.Invoke(e)))
                    {
                        isAllocated = true;

                        then.Invoke(requestable, e);

                        return file.UploadFile(method, fileName, timeout);
                    }

                    throw;
                }
            }

            public void DownloadFile(string fileName, int timeout = 5000)
            {
                if (isAllocated)
                {
                    file.DownloadFile(fileName, timeout);
                }
                else
                {
                    try
                    {
                        file.DownloadFile(fileName, timeout);
                    }
                    catch (WebException e)
                    {
                        if (predicates.All(x => x.Invoke(e)))
                        {
                            isAllocated = true;

                            then.Invoke(requestable, e);

                            file.DownloadFile(fileName, timeout);
                        }

                        throw;
                    }
                }
            }

            byte[] IFileRequestable.UploadFile(string fileName, int timeout)
            {
                try
                {
                    return UploadFile(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            byte[] IFileRequestable.UploadFile(string method, string fileName, int timeout)
            {
                try
                {
                    return UploadFile(method, fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            void IFileRequestable.DownloadFile(string fileName, int timeout)
            {
                try
                {
                    DownloadFile(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            public IThenAndConditionRequestable If(Predicate<WebException> match)
            {
                if (match is null)
                {
                    throw new ArgumentNullException(nameof(match));
                }

                predicates.Add(match);

                return this;
            }

            public IThenAndConditionRequestable And(Predicate<WebException> match)
            {
                if (match is null)
                {
                    throw new ArgumentNullException(nameof(match));
                }

                predicates.Add(match);

                return this;
            }


#if !NET40
            public override async Task<string> RequestImplementAsync(string method, int timeout = 5000)
            {
                if (isAllocated)
                {
                    return await request.RequestImplementAsync(method, timeout);
                }

                try
                {
                    return await request.RequestImplementAsync(method, timeout);
                }
                catch (WebException e)
                {
                    if (predicates.All(x => x.Invoke(e)))
                    {
                        isAllocated = true;

                        then.Invoke(requestable, e);

                        return await request.RequestImplementAsync(method, timeout);
                    }

                    throw;
                }
            }

            public async Task<byte[]> UploadFileAsync(string fileName, int timeout = 5000) => await UploadFileAsync(null, fileName, timeout);

            public async Task<byte[]> UploadFileAsync(string method, string fileName, int timeout = 5000)
            {
                if (isAllocated)
                {
                    return await file.UploadFileAsync(method, fileName, timeout);
                }

                try
                {
                    return await file.UploadFileAsync(method, fileName, timeout);
                }
                catch (WebException e)
                {
                    if (predicates.All(x => x.Invoke(e)))
                    {
                        isAllocated = true;

                        then.Invoke(requestable, e);

                        return await file.UploadFileAsync(method, fileName, timeout);
                    }

                    throw;
                }
            }

            public async Task DownloadFileAsync(string fileName, int timeout = 5000)
            {
                if (isAllocated)
                {
                    await file.DownloadFileAsync(fileName, timeout);
                }
                else
                {
                    try
                    {
                        await file.DownloadFileAsync(fileName, timeout);
                    }
                    catch (WebException e)
                    {
                        if (predicates.All(x => x.Invoke(e)))
                        {
                            isAllocated = true;

                            then.Invoke(requestable, e);

                            await file.DownloadFileAsync(fileName, timeout);
                        }

                        throw;
                    }
                }
            }

            async Task<byte[]> IFileRequestable.UploadFileAsync(string fileName, int timeout)
            {
                try
                {
                    return await UploadFileAsync(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            async Task<byte[]> IFileRequestable.UploadFileAsync(string method, string fileName, int timeout)
            {
                try
                {
                    return await UploadFileAsync(method, fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            async Task IFileRequestable.DownloadFileAsync(string fileName, int timeout)
            {
                try
                {
                    await DownloadFileAsync(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

#endif

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    request?.Dispose();
                    file?.Dispose();
                    file = null;
                    then = null;
                    request = null;
                    requestable = null;
                    predicates = null;
                }

                base.Dispose(disposing);
            }
        }

        private class ResultCatchRequestable<T> : RequestableExtend<T>, IResultCatchRequestable<T>
        {
            private Requestable<T> requestable;
            private Func<WebException, T> returnValue;

            public ResultCatchRequestable(Requestable<T> requestable, Func<WebException, T> returnValue)
            {
                this.requestable = requestable;
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));
            }

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);

            public override T RequestImplement(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.RequestImplement(method, timeout);
                }
                catch (WebException e)
                {
                    return returnValue.Invoke(e);
                }
            }

#if !NET40
            public override async Task<T> RequestImplementAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestImplementAsync(method, timeout);
                }
                catch (WebException e)
                {
                    return returnValue.Invoke(e);
                }
            }
#endif

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();
                    requestable = null;
                    returnValue = null;
                }

                base.Dispose(disposing);
            }
        }

        private class JsonCatchRequestable<T> : RequestableExtend<T>, IJsonCatchRequestable<T>, IJsonResultCatchRequestable<T>
        {
            private Action<string, Exception> log;
            private Func<string, Exception, T> returnValue;
            private Func<WebException, T> returnValue2;
            private readonly NamingType namingType;
            private Requestable<string> requestable;

            public JsonCatchRequestable(Requestable<string> requestable, Action<string, Exception> log, NamingType namingType)
            {
                if (log is null)
                {
                    throw new ArgumentNullException(nameof(log));
                }

                this.requestable = requestable;
                this.namingType = namingType;
                this.log = log;
            }

            public IResultCatchRequestable<T> JsonCatch(Func<WebException, T> returnValue)
            {
                returnValue2 = returnValue ?? throw new ArgumentNullException(nameof(returnValue));

                return this;
            }

            public IJsonCatchRequestable<T> JsonCatch(Action<string, Exception> log)
            {
                if (log is null)
                {
                    throw new ArgumentNullException(nameof(log));
                }

                this.log += log;

                return this;
            }

            public IJsonResultCatchRequestable<T> JsonCatch(Func<string, Exception, T> returnValue)
            {
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));

                return this;
            }

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);

            public override T RequestImplement(string method, int timeout = 5000)
            {
                string value = default;

                try
                {
                    value = requestable.RequestImplement(method, timeout);
                }
                catch (WebException e)
                {
                    if (returnValue2 is null)
                    {
                        throw;
                    }

                    return returnValue2.Invoke(e);
                }

                try
                {
                    return JsonHelper.Json<T>(value, namingType);
                }
                catch (Exception e) when (IsJsonError(e))
                {
                    log.Invoke(value, e);

                    if (returnValue is null)
                    {
                        throw;
                    }

                    return returnValue.Invoke(value, e);
                }
            }

            private static bool IsJsonError(Exception e)
            {
                for (Type type = e.GetType(), destinationType = typeof(Exception); type != destinationType; type = type.BaseType ?? destinationType)
                {
                    if (type.Name == "JsonException")
                    {
                        return true;
                    }
                }

                return false;
            }

#if !NET40
            public override async Task<T> RequestImplementAsync(string method, int timeout = 5000)
            {
                string value = default;

                try
                {
                    value = await requestable.RequestImplementAsync(method, timeout);
                }
                catch (WebException e)
                {
                    if (returnValue2 is null)
                    {
                        throw;
                    }

                    return returnValue2.Invoke(e);
                }

                try
                {
                    return JsonHelper.Json<T>(value, namingType);
                }
                catch (Exception e) when (IsJsonError(e))
                {
                    log.Invoke(value, e);

                    if (returnValue is null)
                    {
                        throw;
                    }

                    return returnValue.Invoke(value, e);
                }
            }
#endif

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();
                    log = null;
                    requestable = null;
                    returnValue = null;
                    returnValue2 = null;
                }

                base.Dispose(disposing);
            }
        }

        private class JsonResultCatchRequestable<T> : RequestableExtend<T>, IJsonResultCatchRequestable<T>
        {
            private Requestable<string> requestable;
            private Func<string, Exception, T> returnValue;
            private readonly NamingType namingType;

            public JsonResultCatchRequestable(Requestable<string> requestable, Func<string, Exception, T> returnValue, NamingType namingType)
            {
                this.requestable = requestable;
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));
                this.namingType = namingType;
            }

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);

            public override T RequestImplement(string method, int timeout = 5000)
            {
                string value = requestable.RequestImplement(method, timeout);

                try
                {
                    return JsonHelper.Json<T>(value, namingType);
                }
                catch (Exception e) when (IsJsonError(e))
                {
                    return returnValue.Invoke(value, e);
                }
            }

            private static bool IsJsonError(Exception e)
            {
                for (Type type = e.GetType(), destinationType = typeof(Exception); type != destinationType; type = type.BaseType ?? destinationType)
                {
                    if (type.Name == "JsonException")
                    {
                        return true;
                    }
                }

                return false;
            }

#if !NET40
            public override async Task<T> RequestImplementAsync(string method, int timeout = 5000)
            {
                string value = await requestable.RequestImplementAsync(method, timeout);

                try
                {
                    return JsonHelper.Json<T>(value, namingType);
                }
                catch (Exception e) when (IsJsonError(e))
                {
                    return returnValue.Invoke(value, e);
                }
            }
#endif

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();
                    requestable = null;
                    returnValue = null;
                }

                base.Dispose(disposing);
            }
        }

        private class XmlCatchRequestable<T> : RequestableExtend<T>, IXmlCatchRequestable<T>, IXmlResultCatchRequestable<T>
        {
            private Action<string, XmlException> log;
            private Func<string, XmlException, T> returnValue;
            private Func<WebException, T> returnValue2;
            private Requestable<string> requestable;

            public XmlCatchRequestable(Requestable<string> requestable, Action<string, XmlException> log)
            {
                if (log is null)
                {
                    throw new ArgumentNullException(nameof(log));
                }
                this.log = log;
                this.requestable = requestable;
            }

            public IResultCatchRequestable<T> XmlCatch(Func<WebException, T> returnValue)
            {
                returnValue2 = returnValue ?? throw new ArgumentNullException(nameof(returnValue));

                return this;
            }

            public IXmlCatchRequestable<T> XmlCatch(Action<string, XmlException> log)
            {
                this.log += log;

                return this;
            }

            public IXmlResultCatchRequestable<T> XmlCatch(Func<string, XmlException, T> returnValue)
            {
                if (returnValue is null)
                {
                    throw new ArgumentNullException(nameof(returnValue));
                }

                this.returnValue = returnValue;

                return this;
            }

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);

            public override T RequestImplement(string method, int timeout = 5000)
            {
                string value = default;

                try
                {
                    value = requestable.RequestImplement(method, timeout);
                }
                catch (WebException e)
                {
                    if (returnValue2 is null)
                    {
                        throw;
                    }

                    return returnValue2.Invoke(e);
                }

                try
                {
                    return XmlHelper.XmlDeserialize<T>(value);
                }
                catch (XmlException e)
                {
                    log.Invoke(value, e);

                    if (returnValue is null)
                    {
                        throw;
                    }

                    return returnValue.Invoke(value, e);
                }
            }

#if !NET40
            public override async Task<T> RequestImplementAsync(string method, int timeout = 5000)
            {
                string value = default;

                try
                {
                    value = await requestable.RequestImplementAsync(method, timeout);
                }
                catch (WebException e)
                {
                    if (returnValue2 is null)
                    {
                        throw;
                    }

                    return returnValue2.Invoke(e);
                }

                try
                {
                    return XmlHelper.XmlDeserialize<T>(value);
                }
                catch (XmlException e)
                {
                    log.Invoke(value, e);

                    if (returnValue is null)
                    {
                        throw;
                    }

                    return returnValue.Invoke(value, e);
                }
            }
#endif

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();
                    log = null;
                    requestable = null;
                    returnValue = null;
                    returnValue2 = null;
                }

                base.Dispose(disposing);
            }
        }

        private class XmlResultCatchRequestable<T> : RequestableExtend<T>, IXmlResultCatchRequestable<T>
        {
            private Requestable<string> requestable;
            private Func<string, XmlException, T> returnValue;

            public XmlResultCatchRequestable(Requestable<string> requestable, Func<string, XmlException, T> returnValue)
            {
                this.requestable = requestable;
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));
            }

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);

            public override T RequestImplement(string method, int timeout = 5000)
            {
                string value = requestable.RequestImplement(method, timeout);

                try
                {
                    return XmlHelper.XmlDeserialize<T>(value);
                }
                catch (XmlException e)
                {
                    return returnValue.Invoke(value, e);
                }
            }

#if !NET40
            public override async Task<T> RequestImplementAsync(string method, int timeout = 5000)
            {
                string value = await requestable.RequestImplementAsync(method, timeout);

                try
                {
                    return XmlHelper.XmlDeserialize<T>(value);
                }
                catch (XmlException e)
                {
                    return returnValue.Invoke(value, e);
                }
            }
#endif

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();
                    requestable = null;
                    returnValue = null;
                }

                base.Dispose(disposing);
            }
        }

        private class Finallyequestable<T> : RequestableExtend<T>, IFinallyRequestable<T>
        {
            private Requestable<T> requestable;
            private Action log;

            public Finallyequestable(Requestable<T> requestable, Action log)
            {
                this.requestable = requestable;
                this.log = log ?? throw new ArgumentNullException(nameof(log));
            }

            public override T RequestImplement(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.RequestImplement(method, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }
#if !NET40
            public override async Task<T> RequestImplementAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestImplementAsync(method, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }
#endif

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();
                    requestable = null;
                    log = null;
                }

                base.Dispose(disposing);
            }
        }

        private class FinallyStringRequestable : RequestableExtend<string>, IFinallyStringRequestable
        {
            private Requestable<string> requestable;
            private Action log;

            public FinallyStringRequestable(Requestable<string> requestable, Action log)
            {
                this.requestable = requestable;
                this.log = log ?? throw new ArgumentNullException(nameof(log));
            }

            public override string RequestImplement(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.RequestImplement(method, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }
#if !NET40
            public override async Task<string> RequestImplementAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestImplementAsync(method, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }
#endif

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable?.Dispose();
                    requestable = null;
                    log = null;
                }

                base.Dispose(disposing);
            }
        }
        #endregion

        private class Requestable : RequestableExtend<string>, IRequestable, IDisposableFileRequestable
        {
            private Uri __uri;
            private string __data;
            private bool isFormSubmit = false;
            private NameValueCollection __form;
            private Dictionary<string, string> __headers;

            public Requestable(string uriString) : this(new Uri(uriString)) { }
            public Requestable(Uri uri)
            {
                __uri = uri ?? throw new ArgumentNullException(nameof(uri));
                __headers = new Dictionary<string, string>();
            }
            public IRequestable AssignHeader(string header, string value)
            {
                if (header is null)
                {
                    throw new ArgumentNullException(nameof(header));
                }

                __headers[header] = value ?? string.Empty;

                return this;
            }
            public IRequestable AssignHeaders(IEnumerable<KeyValuePair<string, string>> headers)
            {
                foreach (var kv in headers)
                {
                    __headers[kv.Key] = kv.Value;
                }

                return this;
            }
            public IRequestable Body(string body, string contentType)
            {
                if (contentType is null)
                {
                    throw new ArgumentNullException(nameof(contentType));
                }

                __data = body ?? throw new ArgumentNullException(nameof(body));

                return AssignHeader("Content-Type", contentType);
            }
            public IRequestable Xml(string param) => Body(param, "application/xml");
            public IRequestable Xml<T>(T param) where T : class => Xml(XmlHelper.XmlSerialize(param));
            public IRequestable Form(string param, NamingType namingType = NamingType.Normal)
                => Form(JsonHelper.Json<Dictionary<string, string>>(param, namingType));
            public IRequestable Form(IEnumerable<KeyValuePair<string, string>> param, NamingType namingType = NamingType.Normal)
            {
                isFormSubmit = true;

                __form = __form ?? new NameValueCollection();

                if (namingType == NamingType.Normal)
                {
                    foreach (var kv in param)
                    {
                        __form.Add(kv.Key, kv.Value);
                    }
                }
                else
                {
                    foreach (var kv in param)
                    {
                        __form.Add(kv.Key.ToNamingCase(namingType), kv.Value);
                    }
                }

                return AssignHeader("Content-Type", "application/x-www-form-urlencoded");
            }
            public IRequestable Form(IEnumerable<KeyValuePair<string, DateTime>> param, NamingType namingType = NamingType.Normal)
                => Form(param.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFK"))), namingType);
            public IRequestable Form<T>(IEnumerable<KeyValuePair<string, T>> param, NamingType namingType = NamingType.Normal)
                => Form(param.Select(x => new KeyValuePair<string, string>(x.Key, x.Value?.ToString())), namingType);
            public IRequestable Form(object param)
            {
                isFormSubmit = true;

                if (param is null)
                {
                    return this;
                }

                __form = __form ?? new NameValueCollection();

                var typeStore = RuntimeTypeCache.Instance.GetCache(param.GetType());

                foreach (var storeItem in typeStore.PropertyStores)
                {
                    var value = storeItem.Member.GetValue(param, null);

                    if (value is null)
                    {
                        continue;
                    }

                    if (value is DateTime date)
                    {
                        __form.Add(storeItem.Naming, date.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFK"));
                    }
                    else
                    {
                        __form.Add(storeItem.Naming, value.ToString());
                    }
                }

                return AssignHeader("Content-Type", "application/x-www-form-urlencoded");
            }
            public IRequestable Form(object param, NamingType namingType = NamingType.Normal)
            {
                isFormSubmit = true;

                if (param is null)
                {
                    return this;
                }

                __form = __form ?? new NameValueCollection();

                var typeStore = RuntimeTypeCache.Instance.GetCache(param.GetType());

                foreach (var storeItem in typeStore.PropertyStores)
                {
                    var value = storeItem.Member.GetValue(param, null);

                    if (value is null)
                    {
                        continue;
                    }

                    if (value is DateTime date)
                    {
                        __form.Add(storeItem.Name.ToNamingCase(namingType), date.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFK"));
                    }
                    else
                    {
                        __form.Add(storeItem.Name.ToNamingCase(namingType), value.ToString());
                    }
                }

                return AssignHeader("Content-Type", "application/x-www-form-urlencoded");
            }
            public IRequestable Json(string param) => Body(param, "application/json");
            public IRequestable Json<T>(T param, NamingType namingType = NamingType.Normal) where T : class
                => Json(JsonHelper.ToJson(param, namingType));
            public IRequestable AppendQueryString(string param)
            {
                if (string.IsNullOrEmpty(param))
                {
                    return this;
                }
                string query = param.TrimStart('?', '&');

                if (__uri.Query.IsEmpty())
                {
                    string uriString = __uri.ToString();

                    __uri = new Uri(string.Concat(uriString, uriString.IndexOf('?') == -1 ? "?" : "&", query));

                    return this;
                }
                else
                {
                    var dic = new Dictionary<string, string>();

                    foreach (var arg in __uri.Query.Substring(1).Split('&'))
                    {
                        string[] keys = arg.Split('=');

                        dic[keys.First()] = string.Join("=", keys.Skip(1));
                    }

                    foreach (var arg in query.Split('&'))
                    {
                        string[] keys = arg.Split('=');

                        dic[keys.First()] = string.Join("=", keys.Skip(1));
                    }

                    string urlStr = __uri.ToString();

                    int indexOf = urlStr.IndexOf('?');

                    __uri = new Uri(string.Concat(urlStr.Substring(0, indexOf + 1), string.Join("&", dic.Select(x => string.Concat(x.Key, "=", x.Value)))));

                    return this;
                }
            }
            public IRequestable AppendQueryString(string name, string value)
            {
                if (value is null)
                {
                    return this;
                }

                return AppendQueryString(string.Concat(name, "=", value));
            }
            public IRequestable AppendQueryString(string name, DateTime value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => AppendQueryString(string.Concat(name, "=", HttpUtility.UrlEncode(value.ToString(dateFormatString), Encoding.UTF8)));
            public IRequestable AppendQueryString<T>(string name, T value) => AppendQueryString(name, value?.ToString());
            public IRequestable AppendQueryString(IEnumerable<string> param)
              => AppendQueryString(string.Join("&", param));
            public IRequestable AppendQueryString(IEnumerable<KeyValuePair<string, string>> param)
                 => AppendQueryString(string.Join("&", param.Select(kv => string.Concat(kv.Key, "=", kv.Value))));
            public IRequestable AppendQueryString(IEnumerable<KeyValuePair<string, DateTime>> param, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK")
                  => AppendQueryString(string.Join("&", param.Select(x => string.Concat(x.Key, "=", HttpUtility.UrlEncode(x.Value.ToString(dateFormatString), Encoding.UTF8)))));
            public IRequestable AppendQueryString<T>(IEnumerable<KeyValuePair<string, T>> param)
                  => AppendQueryString(string.Join("&", param.Select(x => string.Concat(x.Key, "=", x.Value?.ToString() ?? string.Empty))));
            public IRequestable AppendQueryString(object param, NamingType namingType = NamingType.UrlCase, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK")
            {
                if (param is null)
                {
                    return this;
                }

                var typeStore = RuntimeTypeCache.Instance.GetCache(param.GetType());

                var sb = new StringBuilder();

                foreach (var storeItem in typeStore.PropertyStores.Where(x => x.CanRead))
                {
                    var value = storeItem.Member.GetValue(param, null);

                    if (value is null)
                    {
                        continue;
                    }

                    sb.Append("&")
                        .Append(storeItem.Name.ToNamingCase(namingType))
                        .Append("=");

                    if (value is DateTime date)
                    {
                        sb.Append(HttpUtility.UrlEncode(date.ToString(dateFormatString), Encoding.UTF8));
                    }
                    else
                    {
                        sb.Append(value.ToString());
                    }
                }

                return AppendQueryString(sb.ToString());
            }
            public override string RequestImplement(string method, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    if (method.ToUpper() == "GET")
                    {
                        return client.DownloadString(__uri);
                    }

                    if (isFormSubmit)
                    {
                        if (__form is null)
                        {
                            throw new NotSupportedException("使用表单提交，但未指定表单数据!");
                        }

                        return Encoding.UTF8.GetString(client.UploadValues(__uri, method.ToUpper(), __form));
                    }

                    return client.UploadString(__uri, method.ToUpper(), __data ?? string.Empty);
                }
            }

            public IJsonRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class => new JsonRequestable<T>(this, namingType);
            public IXmlRequestable<T> XmlCast<T>() where T : class => new XmlRequestable<T>(this);
            public IJsonRequestable<T> JsonCast<T>(T _, NamingType namingType = NamingType.Normal) where T : class => new JsonRequestable<T>(this, namingType);
            public IXmlRequestable<T> XmlCast<T>(T _) where T : class => new XmlRequestable<T>(this);

            public IThenRequestable TryIf(Predicate<WebException> match) => new IIFThenRequestable(this, match);

            public IThenConditionRequestable TryThen(Action<IRequestableBase, WebException> then) => new ThenRequestable(this, then);

            public ICatchRequestable WebCatch(Action<WebException> log) => new CatchRequestable(this, this, log);

            public IResultStringCatchRequestable WebCatch(Func<WebException, string> returnValue) => new ResultStringCatchRequestable(this, returnValue);

            public IFinallyRequestable Finally(Action log) => new FinallyRequestable(this, this, log);

            public byte[] UploadFile(string fileName, int timeout = 5000) => UploadFile(null, fileName, timeout);

            public byte[] UploadFile(string method, string fileName, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    return client.UploadFile(__uri, method, fileName);
                }
            }

            public void DownloadFile(string fileName, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    client.DownloadFile(__uri, fileName);
                }
            }

            byte[] IFileRequestable.UploadFile(string fileName, int timeout)
            {
                try
                {
                    return UploadFile(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            byte[] IFileRequestable.UploadFile(string method, string fileName, int timeout)
            {
                try
                {
                    return UploadFile(method, fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            void IFileRequestable.DownloadFile(string fileName, int timeout)
            {
                try
                {
                    DownloadFile(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

#if !NET40
            public override async Task<string> RequestImplementAsync(string method, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    if (method.ToUpper() == "GET")
                    {
                        return await client.DownloadStringTaskAsync(__uri);
                    }

                    if (isFormSubmit)
                    {
                        if (__form is null)
                        {
                            throw new NotSupportedException("使用表单提交，但未指定表单数据!");
                        }

                        return Encoding.UTF8.GetString(await client.UploadValuesTaskAsync(__uri, method.ToUpper(), __form));
                    }

                    return await client.UploadStringTaskAsync(__uri, method.ToUpper(), __data ?? string.Empty);
                }
            }

            public Task<byte[]> UploadFileAsync(string fileName, int timeout = 5000) => UploadFileAsync(null, fileName, timeout);

            public Task<byte[]> UploadFileAsync(string method, string fileName, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    return client.UploadFileTaskAsync(__uri, method, fileName);
                }
            }

            public Task DownloadFileAsync(string fileName, int timeout = 5000)
            {
                using (var client = new WebCoreClient
                {
                    Timeout = timeout,
                    Encoding = Encoding.UTF8
                })
                {
                    foreach (var kv in __headers)
                    {
                        client.Headers.Add(kv.Key, kv.Value);
                    }

                    return client.DownloadFileTaskAsync(__uri, fileName);
                }
            }

            async Task<byte[]> IFileRequestable.UploadFileAsync(string fileName, int timeout)
            {
                try
                {
                    return await UploadFileAsync(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            async Task<byte[]> IFileRequestable.UploadFileAsync(string method, string fileName, int timeout)
            {
                try
                {
                    return await UploadFileAsync(method, fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

            async Task IFileRequestable.DownloadFileAsync(string fileName, int timeout)
            {
                try
                {
                    await DownloadFileAsync(fileName, timeout);
                }
                finally
                {
                    Dispose();
                }
            }

#endif

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    __uri = null;
                    __data = null;
                    __form = null;
                    __headers = null;
                }

                base.Dispose(disposing);
            }
        }

        private class ResultStringCatchRequestable : RequestableExtend<string>, IResultStringCatchRequestable
        {
            private readonly Requestable<string> requestable;
            private readonly Func<WebException, string> returnValue;

            public ResultStringCatchRequestable(Requestable<string> requestable, Func<WebException, string> returnValue)
            {
                this.requestable = requestable;
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));
            }

            public IFinallyStringRequestable Finally(Action log) => new FinallyStringRequestable(this, log);

            public override string RequestImplement(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.Request(method, timeout);
                }
                catch (WebException e)
                {
                    return returnValue.Invoke(e);
                }
            }

#if !NET40
            public override async Task<string> RequestImplementAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestImplementAsync(method, timeout);
                }
                catch (WebException e)
                {
                    return returnValue.Invoke(e);
                }
            }
#endif
        }

        private class JsonRequestable<T> : RequestableExtend<T>, IJsonRequestable<T>
        {
            private readonly Requestable<string> requestable;

            public JsonRequestable(Requestable<string> requestable, NamingType namingType)
            {
                NamingType = namingType;
                this.requestable = requestable;
            }

            public NamingType NamingType { get; }

            public override T RequestImplement(string method, int timeout = 5000)
            {
                return JsonHelper.Json<T>(requestable.RequestImplement(method, timeout), NamingType);
            }

#if !NET40

            public override async Task<T> RequestImplementAsync(string method, int timeout = 5000)
            {
                return JsonHelper.Json<T>(await requestable.RequestImplementAsync(method, timeout), NamingType);
            }
#endif
            public IJsonCatchRequestable<T> JsonCatch(Action<string, Exception> log) => new JsonCatchRequestable<T>(requestable, log, NamingType);

            public IJsonResultCatchRequestable<T> JsonCatch(Func<string, Exception, T> returnValue) => new JsonResultCatchRequestable<T>(requestable, returnValue, NamingType);

            public IResultCatchRequestable<T> WebCatch(Func<WebException, T> returnValue) => new ResultCatchRequestable<T>(this, returnValue);

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        private class XmlRequestable<T> : RequestableExtend<T>, IXmlRequestable<T>
        {
            private readonly Requestable<string> requestable;

            public XmlRequestable(Requestable<string> requestable)
            {
                this.requestable = requestable;
            }

            public override T RequestImplement(string method, int timeout = 5000)
            {
                return XmlHelper.XmlDeserialize<T>(requestable.RequestImplement(method, timeout));
            }

#if !NET40
            public override async Task<T> RequestImplementAsync(string method, int timeout = 5000)
            {
                return XmlHelper.XmlDeserialize<T>(await requestable.RequestImplementAsync(method, timeout));
            }
#endif

            public IXmlCatchRequestable<T> XmlCatch(Action<string, XmlException> log) => new XmlCatchRequestable<T>(requestable, log);

            public IXmlResultCatchRequestable<T> XmlCatch(Func<string, XmlException, T> returnValue) => new XmlResultCatchRequestable<T>(requestable, returnValue);

            public IResultCatchRequestable<T> WebCatch(Func<WebException, T> returnValue) => new ResultCatchRequestable<T>(this, returnValue);

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    requestable.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// 字符串是否为有效链接
        /// </summary>
        /// <param name="uriString">链接地址</param>
        /// <returns></returns>
        public static bool IsUrl(this string uriString) => UriPattern.IsMatch(uriString);

        /// <summary>
        /// 提供远程请求能力
        /// </summary>
        /// <param name="uriString">请求地址</param>
        /// <returns></returns>
        public static IRequestable AsRequestable(this string uriString)
            => new Requestable(uriString);

        /// <summary>
        /// 提供远程请求能力
        /// </summary>
        /// <param name="uri">请求地址</param>
        /// <returns></returns>
        public static IRequestable AsRequestable(this Uri uri)
            => new Requestable(uri);
    }
}
