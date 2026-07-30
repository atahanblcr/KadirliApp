using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace KadirliApp.Tests.Unit;

/// <summary>
/// Mock'lanmış <c>IRepository&lt;T&gt;.Query()</c> üzerinde EF Core'un async LINQ uzantıları
/// (<c>FirstOrDefaultAsync</c>, <c>AnyAsync</c>, <c>ToListAsync</c>…) çalışsın diye gereken
/// bellek-içi async sorgu sağlayıcısı. Bu uzantılar sağlayıcının <see cref="IAsyncQueryProvider"/>
/// olmasını şart koşar; düz <c>List.AsQueryable()</c> ile çalışmazlar.
///
/// ⚠️ <c>IgnoreQueryFilters()</c> burada NO-OP'tur: EF, sağlayıcı <c>EntityQueryProvider</c>
/// değilse kaynağı olduğu gibi döndürür. Global soft-delete filtresinin etkisini simüle etmek
/// gerektiğinde <c>Query()</c> için Moq <c>SetupSequence</c> ile çağrı başına farklı küme dön.
/// </summary>
internal static class TestAsyncQueryableExtensions
{
    public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source) =>
        new TestAsyncEnumerable<T>(source);
}

internal sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(Expression expression) =>
        new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        // TResult daima Task<T> — senkron sonucu hesaplayıp tamamlanmış Task'a sar.
        var resultType = typeof(TResult).GetGenericArguments()[0];

        var result = typeof(IQueryProvider)
            .GetMethods()
            .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
            .MakeGenericMethod(resultType)
            .Invoke(_inner, new object[] { expression });

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { result })!;
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }

    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return default;
    }
}
