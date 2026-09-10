using AtelieBebe.Backoffice.Core.Domain.Entities;
using AtelieBebe.SharedKernel.ValueObjects;

namespace AtelieBebe.Backoffice.Core.Tests.Domain;

public class NewsletterSubscriberTests
{
    [Fact]
    public void Create_StartsActive()
    {
        var subscriber = NewsletterSubscriber.Create(Email.Create("cliente@exemplo.com"));

        Assert.True(subscriber.Active);
    }

    [Fact]
    public void Unsubscribe_SetsActiveToFalse()
    {
        var subscriber = NewsletterSubscriber.Create(Email.Create("cliente@exemplo.com"));

        subscriber.Unsubscribe();

        Assert.False(subscriber.Active);
    }

    [Fact]
    public void Reactivate_AfterUnsubscribe_SetsActiveToTrueAgain()
    {
        var subscriber = NewsletterSubscriber.Create(Email.Create("cliente@exemplo.com"));
        subscriber.Unsubscribe();

        subscriber.Reactivate();

        Assert.True(subscriber.Active);
    }
}
