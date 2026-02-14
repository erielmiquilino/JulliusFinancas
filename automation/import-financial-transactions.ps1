# Script para importar transações financeiras
# Para usar: ajuste o $categoryId e execute o script

$ErrorActionPreference = "Stop"

# ===== CONFIGURAÇÕES =====
$apiBaseUrl = "http://localhost:8081/api/FinancialTransaction"
$categoryId = "your-category-uuid"
$token = "YOUR_JWT_TOKEN_HERE"

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/json"
}

# ===== TRANSAÇÕES (EXEMPLO) =====
$transactions = @(
    @{ Date = "2026-01-01"; Description = "Exemplo de Transação"; Amount = 50.00 }
)

# ===== EXECUÇÃO =====
Write-Host "Iniciando importação de $($transactions.Count) transações financeiras..." -ForegroundColor Cyan
Write-Host "Categoria ID: $categoryId" -ForegroundColor Yellow
Write-Host ""

$successCount = 0
$errorCount = 0

foreach ($transaction in $transactions) {
    $body = @{
        description = $transaction.Description
        amount = $transaction.Amount
        dueDate = $transaction.Date
        type = 0 # PayableBill
        categoryId = $categoryId
        isPaid = $true
        isInstallment = $false
        installmentCount = 1
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
else {
    Write-Host "`nImportação concluída com avisos. Verfique os erros acima." -ForegroundColor Yellow
}
