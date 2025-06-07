import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { CardService } from '../../services/card.service';

@Component({
  selector: 'app-create-card-dialog',
  templateUrl: './create-card-dialog.component.html',
  styleUrls: ['./create-card-dialog.component.scss']
})
export class CreateCardDialogComponent {
  form: FormGroup;



  diaFechamentoOptions = Array.from({ length: 28 }, (_, i) => ({
    value: i + 1,
    label: `Dia ${i + 1}`
  }));

  constructor(
    private fb: FormBuilder,
    private cardService: CardService,
    private dialogRef: MatDialogRef<CreateCardDialogComponent>
  ) {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(50)]],
      IssuingBank: ['', [Validators.required, Validators.maxLength(50)]],
      closingDay: ['', [Validators.required, Validators.min(1), Validators.max(28)]],
      limit: ['', [Validators.required, Validators.min(1)]]
    });
  }

  onSubmit(): void {
    if (this.form.valid) {
      const formValue = this.form.value;

      this.cardService.createCard({
        name: formValue.name,
        issuingBank: formValue.IssuingBank,
        closingDay: Number(formValue.closingDay),
        limit: Number(formValue.limit)
      })
        .subscribe({
          next: () => {
            this.dialogRef.close(true);
          },
          error: (error) => {
            console.error('Erro ao criar cartão:', error);
          }
        });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
