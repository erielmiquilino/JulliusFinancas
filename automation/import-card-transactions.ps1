# Script para importar transações de cartão
# Para usar: ajuste o $cardId e execute o script

$ErrorActionPreference = "Stop"

# ===== CONFIGURAÇÕES =====
$apiBaseUrl = "https://your-api-url.com/api/CardTransaction"
$cardId = "your-card-uuid"
$invoiceYear = 2025
$invoiceMonth = 10
$token = "YOUR_JWT_TOKEN_HERE"

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/json"
}

# ===== TRANSAÇÕES (EXEMPLO) =====
$transactions = @(
    @{ Date = "2025-01-01"; Description = "Exemplo de Compra"; Amount = 100.00; Type = 0 }
)

# ===== EXECUÇÃO =====
Write-Host "Iniciando importação de $($transactions.Count) transações..." -ForegroundColor Cyan
Write-Host "Cartão ID: $cardId" -ForegroundColor Yellow
Write-Host "Fatura: $invoiceMonth/$invoiceYear" -ForegroundColor Yellow
Write-Host ""

$successCount = 0
$errorCount = 0

foreach ($transaction in $transactions) {
    $body = @{
        cardId = $cardId
        description = $transaction.Description
        amount = $transaction.Amount
        date = $transaction.Date
        isInstallment = $false
        installmentCount = 1
        type = $transaction.Type
        invoiceYear = $invoiceYear
        invoiceMonth = $invoiceMonth
    } | ConvertTo-Json

    try {
        Write-Host "Enviando: $($transaction.Description) - R$ $($transaction.Amount)" -NoNewline
        
        $response = Invoke-RestMethod -Uri $apiBaseUrl -Method Post -Body $body -Headers $headers
        
        Write-Host " ✓" -ForegroundColor Green
        $successCount++
    }
    catch {
        Write-Host " ✗" -ForegroundColor Red
        Write-Host "ERRO: $_" -ForegroundColor Red
        $errorCount++
        throw "Erro ao importar transação: $($transaction.Description). Execução interrompida."
    }
}

Write-Host ""
Write-Host "===== RESUMO =====" -ForegroundColor Cyan
Write-Host "Sucessos: $successCount" -ForegroundColor Green
Write-Host "Erros: $errorCount" -ForegroundColor Red
Write-Host "Total: $($transactions.Count)" -ForegroundColor Yellow

if ($errorCount -eq 0) {
    Write-Host "`nImportação concluída com sucesso! ✓" -ForegroundColor Green
}

