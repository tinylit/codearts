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
            public abstract T Request(string method, int timeout = 5000);

#if !NET40
            public Task<T> GetAsync(int timeout = 5000) => RequestAsync("GET", timeout);


            public Task<T> DeleteAsync(int timeout = 5000) => RequestAsync("DELETE", timeout);


            public Task<T> PostAsync(int timeout = 5000) => RequestAsync("POST", timeout);


            public Task<T> PutAsync(int timeout = 5000) => RequestAsync("PUT", timeout);


            public Task<T> HeadAsync(int timeout = 5000) => RequestAsync("HEAD", timeout);


            public Task<T> PatchAsync(int timeout = 5000) => RequestAsync("PATCH", timeout);

            public abstract Task<T> RequestAsync(string method, int timeout = 5000);
#endif
        }

        #region 补充
        private class CatchRequestable : Requestable<string>, ICatchRequestable, IResultStringCatchRequestable
        {
            private Action<WebException> log;
            private Func<WebException, string> returnValue;
            private readonly IRequestable<string> requestable;
            private readonly IFileRequestable file;

            public CatchRequestable(IRequestable<string> requestable, IFileRequestable file, Action<WebException> log)
            {
                this.requestable = requestable;
                this.file = file;
                this.log = log ?? throw new ArgumentNullException(nameof(log));
            }

            public ICatchRequestable Catch(Action<WebException> log)
            {
                if (log is null)
                {
                    throw new ArgumentNullException(nameof(log));
                }

                this.log += log;

                return this;
            }

            public IResultStringCatchRequestable Catch(Func<WebException, string> returnValue)
            {
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));

                return this;
            }

            public IFinallyRequestable Finally(Action log) => new FinallyRequestable(this, file, log);

            IFinallyStringRequestable IResultStringCatchRequestable.Finally(Action log) => new FinallyRequestable(this, file, log);

            public IJsonRequestable<T> JsonCast<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IJsonRequestable<T> JsonCast<T>(T anonymousTypeObject, NamingType namingType = NamingType.CamelCase) where T : class => JsonCast<T>(namingType);

            public override string Request(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.Request(method, timeout);
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
#if !NET40
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestAsync(method, timeout);
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
                    return await file.UploadFileAsync(fileName, timeout);
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
#endif

            public IXmlRequestable<T> XmlCast<T>() where T : class => new XmlRequestable<T>(this);

            public IXmlRequestable<T> XmlCast<T>(T _) where T : class => XmlCast<T>();
        }

        private class FinallyRequestable : Requestable<string>, IFinallyRequestable, IFinallyStringRequestable
        {
            private readonly Action log;
            private readonly IRequestable<string> requestable;
            private readonly IFileRequestable file;

            public FinallyRequestable(IRequestable<string> requestable, IFileRequestable file, Action log)
            {
                this.requestable = requestable;
                this.file = file;
                this.log = log ?? throw new ArgumentNullException(nameof(log));
            }

            public IJsonRequestable<T> JsonCast<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IJsonRequestable<T> JsonCast<T>(T anonymousTypeObject, NamingType namingType = NamingType.CamelCase) where T : class => JsonCast<T>(namingType);

            public override string Request(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.Request(method, timeout);
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
#if !NET40
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestAsync(method, timeout);
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
                    return await file.UploadFileAsync(fileName, timeout);
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
#endif

            public IXmlRequestable<T> XmlCast<T>() where T : class => new XmlRequestable<T>(this);

            public IXmlRequestable<T> XmlCast<T>(T anonymousTypeObject) where T : class => XmlCast<T>();
        }

        private class IIFThenRequestable : Requestable<string>, IThenRequestable, IRetryThenRequestable, IRetryIntervalThenRequestable
        {
            private int maxRetires = 1;
            private int millisecondsTimeout = -1;
            private Func<WebException, int, int> interval;

            private readonly IRequestable<string> request;
            private readonly IFileRequestable file;
            private readonly List<Predicate<WebException>> predicates;

            public IIFThenRequestable(IRequestable<string> request, IFileRequestable file, Predicate<WebException> predicate)
            {
                if (predicate is null)
                {
                    throw new ArgumentNullException(nameof(predicate));
                }

                this.request = request;
                this.file = file;
                predicates = new List<Predicate<WebException>> { predicate };
            }

            public IIFThenRequestable(IRequestable requestable, Predicate<WebException> predicate) : this(requestable, requestable, predicate)
            {
            }

            public IFinallyRequestable Finally(Action log) => new FinallyRequestable(this, file, log);

            public IThenRequestable Or(Predicate<WebException> match)
            {
                if (match is null)
                {
                    throw new ArgumentNullException(nameof(match));
                }

                predicates.Add(match);

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

            public override string Request(string method, int timeout = 5000)
            {
                int times = 0;
                do
                {
                    try
                    {
                        return request.Request(method, timeout);
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

            public ICatchRequestable Catch(Action<WebException> log) => new CatchRequestable(this, file, log);

            public IResultStringCatchRequestable Catch(Func<WebException, string> returnValue) => new ResultStringCatchRequestable(this, returnValue);

            public IJsonRequestable<T> JsonCast<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IXmlRequestable<T> XmlCast<T>() where T : class => new XmlRequestable<T>(this);

            public IJsonRequestable<T> JsonCast<T>(T _, NamingType namingType = NamingType.CamelCase) where T : class => JsonCast<T>(namingType);

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

#if !NET40
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
            {
                int times = 0;

                do
                {
                    try
                    {
                        return await request.RequestAsync(method, timeout);
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
#endif
        }

        private class ThenRequestable : Requestable<string>, IThenConditionRequestable, IThenAndConditionRequestable
        {
            private volatile bool isAllocated = false;

            private readonly IRequestableBase requestable;
            private readonly IRequestable<string> request;
            private readonly IFileRequestable file;
            private readonly Action<IRequestableBase, WebException> then;
            private readonly List<Predicate<WebException>> predicates = new List<Predicate<WebException>>();

            private ThenRequestable(IRequestable<string> request, IFileRequestable file, IRequestableBase requestable, Action<IRequestableBase, WebException> then)
            {
                this.request = request;
                this.requestable = requestable;
                this.file = file;
                this.then = then ?? throw new ArgumentNullException(nameof(then));
            }

            public ThenRequestable(IRequestable requestable, Action<IRequestableBase, WebException> then) : this(requestable, requestable, requestable, then)
            {
            }

            public IFinallyRequestable Finally(Action log) => new FinallyRequestable(this, file, log);

            public IThenRequestable TryIf(Predicate<WebException> match) => new IIFThenRequestable(this, this, match);

            public IThenConditionRequestable Then(Action<IRequestableBase, WebException> then) => new ThenRequestable(this, this, requestable, then);

            public override string Request(string method, int timeout = 5000)
            {
                if (isAllocated)
                {
                    return request.Request(method, timeout);
                }

                try
                {
                    return request.Request(method, timeout);
                }
                catch (WebException e)
                {
                    if (predicates.All(x => x.Invoke(e)))
                    {
                        isAllocated = true;

                        then.Invoke(requestable, e);

                        return request.Request(method, timeout);
                    }

                    throw;
                }
            }

            public ICatchRequestable Catch(Action<WebException> log) => new CatchRequestable(this, file, log);

            public IResultStringCatchRequestable Catch(Func<WebException, string> returnValue) => new ResultStringCatchRequestable(this, returnValue);

            public IJsonRequestable<T> JsonCast<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);

            public IXmlRequestable<T> XmlCast<T>() where T : class => new XmlRequestable<T>(this);

            public IJsonRequestable<T> JsonCast<T>(T _, NamingType namingType = NamingType.CamelCase) where T : class => JsonCast<T>(namingType);

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
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
            {
                if (isAllocated)
                {
                    return await request.RequestAsync(method, timeout);
                }

                try
                {
                    return await request.RequestAsync(method, timeout);
                }
                catch (WebException e)
                {
                    if (predicates.All(x => x.Invoke(e)))
                    {
                        isAllocated = true;

                        then.Invoke(requestable, e);

                        return await request.RequestAsync(method, timeout);
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
#endif
        }

        private class ResultCatchRequestable<T> : Requestable<T>, IResultCatchRequestable<T>
        {
            private readonly IRequestable<T> requestable;
            private readonly Func<WebException, T> returnValue;

            public ResultCatchRequestable(IRequestable<T> requestable, Func<WebException, T> returnValue)
            {
                this.requestable = requestable;
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));
            }

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);

            public override T Request(string method, int timeout = 5000)
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
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestAsync(method, timeout);
                }
                catch (WebException e)
                {
                    return returnValue.Invoke(e);
                }
            }
#endif

        }

        private class JsonCatchRequestable<T> : Requestable<T>, IJsonCatchRequestable<T>, IJsonResultCatchRequestable<T>
        {
            private Action<string, Exception> log;
            private Func<string, Exception, T> returnValue;
            private Func<WebException, T> returnValue2;
            private readonly NamingType namingType;
            private readonly IRequestable<string> requestable;

            public JsonCatchRequestable(IRequestable<string> requestable, Action<string, Exception> log, NamingType namingType)
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

            public override T Request(string method, int timeout = 5000)
            {
                string value = default;

                try
                {
                    value = requestable.Request(method, timeout);
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
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                string value = default;

                try
                {
                    value = await requestable.RequestAsync(method, timeout);
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

        }

        private class JsonResultCatchRequestable<T> : Requestable<T>, IJsonResultCatchRequestable<T>
        {
            private readonly IRequestable<string> requestable;
            private readonly Func<string, Exception, T> returnValue;
            private readonly NamingType namingType;

            public JsonResultCatchRequestable(IRequestable<string> requestable, Func<string, Exception, T> returnValue, NamingType namingType)
            {
                this.requestable = requestable;
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));
                this.namingType = namingType;
            }

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);

            public override T Request(string method, int timeout = 5000)
            {
                string value = requestable.Request(method, timeout);

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
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                string value = await requestable.RequestAsync(method, timeout);

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

        }

        private class XmlCatchRequestable<T> : Requestable<T>, IXmlCatchRequestable<T>, IXmlResultCatchRequestable<T>
        {
            private Action<string, XmlException> log;
            private Func<string, XmlException, T> returnValue;
            private Func<WebException, T> returnValue2;
            private readonly IRequestable<string> requestable;

            public XmlCatchRequestable(IRequestable<string> requestable, Action<string, XmlException> log)
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

            public override T Request(string method, int timeout = 5000)
            {
                string value = default;

                try
                {
                    value = requestable.Request(method, timeout);
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
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                string value = default;

                try
                {
                    value = await requestable.RequestAsync(method, timeout);
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

        }

        private class XmlResultCatchRequestable<T> : Requestable<T>, IXmlResultCatchRequestable<T>
        {
            private readonly IRequestable<string> requestable;
            private readonly Func<string, XmlException, T> returnValue;

            public XmlResultCatchRequestable(IRequestable<string> requestable, Func<string, XmlException, T> returnValue)
            {
                this.requestable = requestable;
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));
            }

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);

            public override T Request(string method, int timeout = 5000)
            {
                string value = requestable.Request(method, timeout);

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
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                string value = await requestable.RequestAsync(method, timeout);

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
        }

        private class Finallyequestable<T> : Requestable<T>, IFinallyRequestable<T>
        {
            private readonly IRequestable<T> requestable;
            private readonly Action log;

            public Finallyequestable(IRequestable<T> requestable, Action log)
            {
                this.requestable = requestable;
                this.log = log ?? throw new ArgumentNullException(nameof(log));
            }

            public override T Request(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.Request(method, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }
#if !NET40
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestAsync(method, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }
#endif
        }
        private class FinallyStringRequestable : Requestable<string>, IFinallyStringRequestable
        {
            private readonly IRequestable<string> requestable;
            private readonly Action log;

            public FinallyStringRequestable(IRequestable<string> requestable, Action log)
            {
                this.requestable = requestable;
                this.log = log ?? throw new ArgumentNullException(nameof(log));
            }

            public override string Request(string method, int timeout = 5000)
            {
                try
                {
                    return requestable.Request(method, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }
#if !NET40
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestAsync(method, timeout);
                }
                finally
                {
                    log.Invoke();
                }
            }
#endif
        }
        #endregion

        private class Requestable : Requestable<string>, IRequestable
        {
            private Uri __uri;
            private string __data;
            private bool isFormSubmit = false;
            private NameValueCollection __form;
            private readonly Dictionary<string, string> __headers;

            public Requestable(string uriString) : this(new Uri(uriString)) { }
            public Requestable(Uri uri)
            {
                __uri = uri ?? throw new ArgumentNullException(nameof(uri));
                __headers = new Dictionary<string, string>();
            }
            public IRequestable AssignHeader(string header, string value)
            {
                __headers[header] = value;

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
            public IRequestable Xml(string param)
            {
                __data = param;

                return AssignHeader("Content-Type", "application/xml");
            }
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
            public IRequestable Json(string param)
            {
                __data = param;

                return AssignHeader("Content-Type", "application/json");
            }
            public IRequestable Json<T>(T param, NamingType namingType = NamingType.CamelCase) where T : class
                => Json(JsonHelper.ToJson(param, namingType));
            public IRequestable AppendQueryString(string param)
            {
                if (string.IsNullOrEmpty(param))
                {
                    return this;
                }

                string uriString = __uri.ToString();

                string query = param.TrimStart('?', '&');

                __uri = new Uri(string.Concat(uriString, uriString.IndexOf('?') == -1 ? "?" : "&", query));

                return this;
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
            public override string Request(string method, int timeout = 5000)
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

            public IJsonRequestable<T> JsonCast<T>(NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);
            public IXmlRequestable<T> XmlCast<T>() where T : class => new XmlRequestable<T>(this);
            public IJsonRequestable<T> JsonCast<T>(T _, NamingType namingType = NamingType.CamelCase) where T : class => new JsonRequestable<T>(this, namingType);
            public IXmlRequestable<T> XmlCast<T>(T _) where T : class => new XmlRequestable<T>(this);

            public IThenRequestable TryIf(Predicate<WebException> match) => new IIFThenRequestable(this, match);

            public IThenConditionRequestable TryThen(Action<IRequestableBase, WebException> then) => new ThenRequestable(this, then);

            public ICatchRequestable Catch(Action<WebException> log) => new CatchRequestable(this, this, log);

            public IResultStringCatchRequestable Catch(Func<WebException, string> returnValue) => new ResultStringCatchRequestable(this, returnValue);

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

#if !NET40
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
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
#endif
        }

        private class ResultStringCatchRequestable : Requestable<string>, IResultStringCatchRequestable
        {
            private readonly IRequestable<string> requestable;
            private readonly Func<WebException, string> returnValue;

            public ResultStringCatchRequestable(IRequestable<string> requestable, Func<WebException, string> returnValue)
            {
                this.requestable = requestable;
                this.returnValue = returnValue ?? throw new ArgumentNullException(nameof(returnValue));
            }

            public IFinallyStringRequestable Finally(Action log) => new FinallyStringRequestable(this, log);

            public override string Request(string method, int timeout = 5000)
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
            public override async Task<string> RequestAsync(string method, int timeout = 5000)
            {
                try
                {
                    return await requestable.RequestAsync(method, timeout);
                }
                catch (WebException e)
                {
                    return returnValue.Invoke(e);
                }
            }
#endif
        }

        private class JsonRequestable<T> : Requestable<T>, IJsonRequestable<T>
        {
            private readonly IRequestable<string> requestable;

            public JsonRequestable(IRequestable<string> requestable, NamingType namingType)
            {
                NamingType = namingType;
                this.requestable = requestable;
            }

            public NamingType NamingType { get; }

            public override T Request(string method, int timeout = 5000)
            {
                return JsonHelper.Json<T>(requestable.Request(method, timeout), NamingType);
            }

#if !NET40

            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                return JsonHelper.Json<T>(await requestable.RequestAsync(method, timeout), NamingType);
            }
#endif
            public IJsonCatchRequestable<T> JsonCatch(Action<string, Exception> log) => new JsonCatchRequestable<T>(requestable, log, NamingType);

            public IJsonResultCatchRequestable<T> JsonCatch(Func<string, Exception, T> returnValue) => new JsonResultCatchRequestable<T>(requestable, returnValue, NamingType);

            public IResultCatchRequestable<T> Catch(Func<WebException, T> returnValue) => new ResultCatchRequestable<T>(this, returnValue);

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);
        }

        private class XmlRequestable<T> : Requestable<T>, IXmlRequestable<T>
        {
            private readonly IRequestable<string> requestable;

            public XmlRequestable(IRequestable<string> requestable)
            {
                this.requestable = requestable;
            }

            public override T Request(string method, int timeout = 5000)
            {
                return XmlHelper.XmlDeserialize<T>(requestable.Request(method, timeout));
            }

#if !NET40
            public override async Task<T> RequestAsync(string method, int timeout = 5000)
            {
                return XmlHelper.XmlDeserialize<T>(await requestable.RequestAsync(method, timeout));
            }
#endif

            public IXmlCatchRequestable<T> XmlCatch(Action<string, XmlException> log) => new XmlCatchRequestable<T>(requestable, log);

            public IXmlResultCatchRequestable<T> XmlCatch(Func<string, XmlException, T> returnValue) => new XmlResultCatchRequestable<T>(requestable, returnValue);

            public IResultCatchRequestable<T> Catch(Func<WebException, T> returnValue) => new ResultCatchRequestable<T>(this, returnValue);

            public IFinallyRequestable<T> Finally(Action log) => new Finallyequestable<T>(this, log);
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
