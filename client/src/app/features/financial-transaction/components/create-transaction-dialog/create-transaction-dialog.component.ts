import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { FinancialTransactionService, TransactionType } from '../../services/financial-transaction.service';

@Component({
  selector: 'app-create-transaction-dialog',
  templateUrl: './create-transaction-dialog.component.html',
  styleUrls: ['./create-transaction-dialog.component.scss']
})
export class CreateTransactionDialogComponent {
  form: FormGroup;
  transactionTypes = [
    { value: TransactionType.PayableBill, label: 'Conta a Pagar' },
    { value: TransactionType.ReceivableBill, label: 'Conta a Receber' }
  ];

  constructor(
    private fb: FormBuilder,
    private transactionService: FinancialTransactionService,
    private dialogRef: MatDialogRef<CreateTransactionDialogComponent>
  ) {
    const today = new Date();
    const localToday = new Date(today.getTime() + today.getTimezoneOffset() * 60000);

    this.form = this.fb.group({
      description: ['', Validators.required],
      amount: ['', [Validators.required, Validators.min(0.01)]],
      dueDate: [localToday, Validators.required],
      type: [TransactionType.PayableBill, Validators.required]
    });
  }

  onSubmit(): void {
    if (this.form.valid) {
      const formValue = this.form.value;
      const dueDate = new Date(formValue.dueDate);
      const utcDueDate = new Date(dueDate.getTime() - dueDate.getTimezoneOffset() * 60000);

      this.transactionService.createTransaction({
        ...formValue,
        dueDate: utcDueDate
      })
        .subscribe({
          next: () => {
            this.dialogRef.close(true);
          },
          error: (error) => {
            console.error('Erro ao criar transação:', error);
          }
        });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
