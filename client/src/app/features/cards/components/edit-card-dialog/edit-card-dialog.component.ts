import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { CardService, Card, UpdateCardRequest } from '../../services/card.service';

@Component({
  selector: 'app-edit-card-dialog',
  templateUrl: './edit-card-dialog.component.html',
  styleUrls: ['./edit-card-dialog.component.scss']
})
export class EditCardDialogComponent {
  form: FormGroup;

  bandeiraOptions = [
    { value: 'Visa', label: 'Visa' },
    { value: 'Mastercard', label: 'Mastercard' },
    { value: 'American Express', label: 'American Express' },
    { value: 'Elo', label: 'Elo' },
    { value: 'Hipercard', label: 'Hipercard' },
    { value: 'Outro', label: 'Outro' }
  ];

  diaFechamentoOptions = Array.from({ length: 28 }, (_, i) => ({
    value: i + 1,
    label: `Dia ${i + 1}`
  }));

  constructor(
    private fb: FormBuilder,
    private cardService: CardService,
    private dialogRef: MatDialogRef<EditCardDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: Card
  ) {
    this.form = this.fb.group({
      name: [data.name, [Validators.required, Validators.maxLength(50)]],
      issuingBank: [data.issuingBank, [Validators.required, Validators.maxLength(50)]],
      closingDay: [data.closingDay, [Validators.required, Validators.min(1), Validators.max(28)]],
      limit: [data.limit, [Validators.required, Validators.min(1)]]
    });
  }

  onSubmit(): void {
    if (this.form.valid) {
      const formValue = this.form.value;

      const updateRequest: UpdateCardRequest = {
        name: formValue.name,
        issuingBank: formValue.issuingBank,
        closingDay: Number(formValue.closingDay),
        limit: Number(formValue.limit)
      };

      this.cardService.updateCard(this.data.id, updateRequest)
        .subscribe({
          next: () => {
            this.dialogRef.close(true);
          },
          error: (error) => {
            console.error('Erro ao atualizar cartão:', error);
          }
        });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
