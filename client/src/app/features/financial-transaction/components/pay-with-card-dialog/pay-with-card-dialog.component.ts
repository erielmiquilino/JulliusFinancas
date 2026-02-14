import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CardService, Card } from '../../../cards/services/card.service';
import { FinancialTransactionService } from '../../services/financial-transaction.service';

export interface PayWithCardDialogData {
  selectedTransactionIds: string[];
  totalAmount: number;
}

@Component({
  selector: 'app-pay-with-card-dialog',
  templateUrl: './pay-with-card-dialog.component.html',
  styleUrls: ['./pay-with-card-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule
  ]
})
export class PayWithCardDialogComponent implements OnInit {
  form: FormGroup;
  cards: Card[] = [];
  selectedTransactionsCount: number;
  totalAmount: number;
  invoiceOptions: { value: string, label: string }[] = [];

  constructor(
    private fb: FormBuilder,
    private cardService: CardService,
    private financialTransactionService: FinancialTransactionService,
    private snackBar: MatSnackBar,
    private dialogRef: MatDialogRef<PayWithCardDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: PayWithCardDialogData
  ) {
    this.selectedTransactionsCount = data.selectedTransactionIds.length;
    this.totalAmount = data.totalAmount;

    this.form = this.fb.group({
      cardId: ['', Validators.required],
      cardAmount: [this.totalAmount, [Validators.required, Validators.min(0.01)]],
      invoicePeriod: ['', Validators.required]
    });

    // Gera as opções de faturas
    this.generateInvoiceOptions();
  }

  ngOnInit(): void {
    this.loadCards();

    // Observa mudanças no campo cardId para calcular a fatura atual do cartão selecionado
    this.form.get('cardId')?.valueChanges.subscribe(cardId => {
      if (cardId) {
        const selectedCard = this.cards.find(c => c.id === cardId);
        if (selectedCard) {
          const currentInvoice = this.calculateCurrentInvoicePeriod(selectedCard.closingDay, selectedCard.dueDay);
          this.form.patchValue({
            invoicePeriod: `${currentInvoice.month}-${currentInvoice.year}`
          });
        }
      }
    });
  }

  loadCards(): void {
    this.cardService.getCards().subscribe({
      next: (cards) => {
        this.cards = cards;
        // Se houver apenas um cartão, seleciona automaticamente
        if (cards.length === 1) {
          this.form.patchValue({ cardId: cards[0].id });
        }
      },
      error: (error) => {
        console.error('Erro ao carregar cartões:', error);
      }
    });
  }

  generateInvoiceOptions(): void {
    const currentDate = new Date();
    const currentMonth = currentDate.getMonth() + 1;
    const currentYear = currentDate.getFullYear();

    this.invoiceOptions = [];

    // Gera 12 meses para o passado + atual + 12 meses para o futuro (25 opções)
    for (let i = -12; i <= 12; i++) {
      const date = new Date(currentYear, currentMonth - 1 + i, 1);
      const month = date.getMonth() + 1;
      const year = date.getFullYear();

      const monthNames = [
        'janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho',
        'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'
      ];

      const display = `${monthNames[date.getMonth()]}/${year}`;

      this.invoiceOptions.push({
        value: `${month}-${year}`,
        label: display
      });
    }
  }

  /**
   * Calcula o período da fatura atual baseado na data de hoje e nos dias de fechamento/vencimento do cartão
   * Replica a lógica do método CalculateInvoicePeriod do backend
   */
  private calculateCurrentInvoicePeriod(closingDay: number, dueDay: number): { year: number, month: number } {
    const today = new Date();

    let effectiveClosingDate: Date;

    if (today.getDate() > closingDay) {
      // Se hoje é depois do dia de fechamento, a data efetiva de fechamento é no próximo mês
      effectiveClosingDate = new Date(today.getFullYear(), today.getMonth() + 1, closingDay);
    } else {
      // Se hoje é antes ou no dia de fechamento, a data efetiva é neste mês
      effectiveClosingDate = new Date(today.getFullYear(), today.getMonth(), closingDay);
    }

    let invoiceDueDate: Date;

    if (dueDay <= closingDay) {
      // Se o vencimento é antes ou no dia de fechamento, vai para o próximo mês
      const monthOfDueDate = new Date(effectiveClosingDate.getFullYear(), effectiveClosingDate.getMonth() + 1, 1);
      invoiceDueDate = new Date(monthOfDueDate.getFullYear(), monthOfDueDate.getMonth(), dueDay);
    } else {
      // Se o vencimento é depois do fechamento, fica no mesmo mês do fechamento
      invoiceDueDate = new Date(effectiveClosingDate.getFullYear(), effectiveClosingDate.getMonth(), dueDay);
    }

    return {
      year: invoiceDueDate.getFullYear(),
      month: invoiceDueDate.getMonth() + 1 // +1 porque getMonth() retorna 0-11
    };
  }

  onSave(): void {
    if (this.form.valid) {
      const formValue = this.form.value;
      const [month, year] = formValue.invoicePeriod.split('-').map(Number);

      const request = {
        transactionIds: this.data.selectedTransactionIds,
        cardId: formValue.cardId,
        cardAmount: formValue.cardAmount,
        invoiceMonth: month,
        invoiceYear: year
      };

      this.financialTransactionService.payWithCard(request).subscribe({
        next: (response) => {
          this.snackBar.open(
            `Pagamento realizado! ${response.paidTransactionsCount} transação(ões) marcada(s) como paga(s).`,
            'Fechar',
            { duration: 5000 }
          );
          this.dialogRef.close(true);
        },
        error: (error) => {
          const errorMessage = error.error?.message || error.error || 'Erro ao processar pagamento';
          this.snackBar.open(
            `Erro: ${errorMessage}`,
            'Fechar',
            { duration: 5000 }
          );
        }
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}

