using System.Text.Json;
using FluentAssertions;
using Jullius.ServiceApi.Application.DTOs;
using Xunit;

namespace Jullius.Tests.Telegram;

/// <summary>
/// Testa a desserialização dos DTOs do Gemini (GeminiClassificationResult, GeminiIntentResponse, GeminiExtractedData).
/// Valida o contrato JSON que o Gemini deve retornar.
/// </summary>
public class GeminiResponseParsingTests
{
    #region GeminiClassificationResult — Multi-transaction parsing

    [Fact]
    public void Deserialize_ShouldParseMultipleTransactions()
    {
        var json = """
        {
          "transactions": [
            {
              "intent": "CREATE_EXPENSE",
              "confidence": 0.95,
              "data": {
                "description": "Almoço no Myata",
                "amount": 22.50,
                "categoryName": "Essenciais",
                "isPaid": true
              },
              "missingFields": [],
              "clarificationQuestion": null
            },
            {
              "intent": "CREATE_EXPENSE",
              "confidence": 0.90,
              "data": {
                "description": "Carregador Samsung",
                "amount": 79,
                "categoryName": "Não planejado",
                "isPaid": true
              },
              "missingFields": [],
              "clarificationQuestion": null
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<GeminiClassificationResult>(json);

        result.Should().NotBeNull();
        result!.Transactions.Should().HaveCount(2);

        result.Transactions[0].Intent.Should().Be("CREATE_EXPENSE");
        result.Transactions[0].Data.Description.Should().Be("Almoço no Myata");
        result.Transactions[0].Data.Amount.Should().Be(22.50m);
        result.Transactions[0].Data.CategoryName.Should().Be("Essenciais");
        result.Transactions[0].Data.IsPaid.Should().BeTrue();

        result.Transactions[1].Data.Description.Should().Be("Carregador Samsung");
        result.Transactions[1].Data.Amount.Should().Be(79m);
        result.Transactions[1].Data.IsPaid.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_ShouldParseSingleTransaction()
    {
        var json = """
        {
          "transactions": [
            {
              "intent": "CREATE_EXPENSE",
              "confidence": 0.95,
                "data": {
                  "description": "Café",
                  "amount": 8.50,
                  "categoryName": "Alimentação",
                  "isPaid": false,
                  "dueDate": "2026-02-16"
                },
                "missingFields": [],
                "clarificationQuestion": null
              }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<GeminiClassificationResult>(json);

        result.Should().NotBeNull();
        result!.Transactions.Should().HaveCount(1);
        result.Transactions[0].Data.IsPaid.Should().BeFalse();
        result.Transactions[0].Data.DueDate.Should().Be(new DateTime(2026, 2, 16));
    }

    [Fact]
    public void Deserialize_ShouldHandleEmptyTransactionsArray()
    {
        var json = """{ "transactions": [] }""";

        var result = JsonSerializer.Deserialize<GeminiClassificationResult>(json);

        result.Should().NotBeNull();
        result!.Transactions.Should().BeEmpty();
    }

    #endregion

    #region Fallback — Legacy Single GeminiIntentResponse

    [Fact]
    public void Deserialize_ShouldParseLegacySingleResponse()
    {
        var json = """
        {
          "intent": "CREATE_EXPENSE",
          "confidence": 0.95,
          "data": {
            "description": "Almoço",
            "amount": 45,
            "categoryName": "Alimentação",
            "isPaid": false
          },
          "missingFields": [],
          "clarificationQuestion": null
        }
        """;

        var result = JsonSerializer.Deserialize<GeminiIntentResponse>(json);

        result.Should().NotBeNull();
        result!.Intent.Should().Be("CREATE_EXPENSE");
        result.Confidence.Should().BeApproximately(0.95, 0.01);
        result.Data.Description.Should().Be("Almoço");
        result.Data.IsPaid.Should().BeFalse();
    }

    #endregion

    #region IsPaid Field

    [Theory]
    [InlineData("""{ "isPaid": true }""", true)]
    [InlineData("""{ "isPaid": false }""", false)]
    [InlineData("""{ }""", null)]
    public void Deserialize_ShouldParseIsPaid_Correctly(string json, bool? expected)
    {
        var result = JsonSerializer.Deserialize<GeminiExtractedData>(json);

        result.Should().NotBeNull();
        result!.IsPaid.Should().Be(expected);
    }

    #endregion

    #region Card Purchase with Installments

    [Fact]
    public void Deserialize_ShouldParseCardPurchaseWithInstallments()
    {
        var json = """
        {
          "transactions": [
            {
              "intent": "CREATE_CARD_PURCHASE",
              "confidence": 0.95,
              "data": {
                "description": "Notebook Lenovo",
                "amount": 3500,
                "cardName": "Nubank",
                "installments": 10,
                "isPaid": false
              },
              "missingFields": [],
              "clarificationQuestion": null
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<GeminiClassificationResult>(json);

        result!.Transactions.Should().HaveCount(1);
        var tx = result.Transactions[0];
        tx.Intent.Should().Be("CREATE_CARD_PURCHASE");
        tx.Data.CardName.Should().Be("Nubank");
        tx.Data.Installments.Should().Be(10);
        tx.Data.Amount.Should().Be(3500m);
    }

    #endregion

    #region Financial Consulting

    [Fact]
    public void Deserialize_ShouldParseFinancialConsulting()
    {
        var json = """
        {
          "transactions": [
            {
              "intent": "FINANCIAL_CONSULTING",
              "confidence": 0.98,
              "data": {
                "question": "Como estou esse mês?"
              },
              "missingFields": [],
              "clarificationQuestion": null
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<GeminiClassificationResult>(json);

        result!.Transactions.Should().HaveCount(1);
        result.Transactions[0].Intent.Should().Be("FINANCIAL_CONSULTING");
        result.Transactions[0].Data.Question.Should().Be("Como estou esse mês?");
    }

    #endregion

    #region Missing Fields

    [Fact]
    public void Deserialize_ShouldParseMissingFields()
    {
        var json = """
        {
          "transactions": [
            {
              "intent": "CREATE_EXPENSE",
              "confidence": 0.85,
              "data": {
                "description": "Almoço",
                "amount": 45
              },
              "missingFields": ["categoryName"],
              "clarificationQuestion": "Em qual categoria devo lançar?"
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<GeminiClassificationResult>(json);

        var tx = result!.Transactions[0];
        tx.MissingFields.Should().ContainSingle().Which.Should().Be("categoryName");
        tx.ClarificationQuestion.Should().Be("Em qual categoria devo lançar?");
    }

    #endregion

    #region Mixed Intents in Batch

    [Fact]
    public void Deserialize_ShouldParseMixedIntentsBatch()
    {
        var json = """
        {
          "transactions": [
            {
              "intent": "CREATE_EXPENSE",
              "confidence": 0.95,
              "data": {
                "description": "Internet",
                "amount": 120,
                "categoryName": "Essenciais",
                "isPaid": true
              },
              "missingFields": []
            },
            {
              "intent": "CREATE_CARD_PURCHASE",
              "confidence": 0.90,
              "data": {
                "description": "Mouse Gamer",
                "amount": 250,
                "cardName": "Inter",
                "installments": 3,
                "isPaid": false
              },
              "missingFields": []
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<GeminiClassificationResult>(json);

        result!.Transactions.Should().HaveCount(2);
        result.Transactions[0].Intent.Should().Be("CREATE_EXPENSE");
        result.Transactions[0].Data.IsPaid.Should().BeTrue();
        result.Transactions[1].Intent.Should().Be("CREATE_CARD_PURCHASE");
        result.Transactions[1].Data.CardName.Should().Be("Inter");
        result.Transactions[1].Data.Installments.Should().Be(3);
    }

    #endregion
}
