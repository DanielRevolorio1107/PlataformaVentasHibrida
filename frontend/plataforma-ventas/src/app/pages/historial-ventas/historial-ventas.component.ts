import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-historial-ventas',
  standalone: true,
  imports: [
    CommonModule,
  ],
  templateUrl: './historial-ventas.component.html',
  styleUrl: './historial-ventas.component.scss'
})
export class HistorialVentasComponent implements OnInit {

  ventas: any[] = [];

  mensaje = '';
  cargando = false;

  private apiUrl = 'http://localhost:5080/api/ventas';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.cargarVentas();
  }

  obtenerHeaders(): HttpHeaders {

    const token = localStorage.getItem('token');

    return new HttpHeaders({
      Authorization: `Bearer ${token}`
    });
  }

  cargarVentas(): void {

    this.cargando = true;
    this.mensaje = '';

    this.http.get<any[]>(
      this.apiUrl,
      {
        headers: this.obtenerHeaders()
      }
    ).subscribe({

      next: (ventas) => {

        this.ventas = ventas;
        this.cargando = false;

      },

      error: (error) => {

        console.error(
          'Error al cargar ventas:',
          error
        );

        this.mensaje =
          'No se pudo cargar el historial de ventas.';

        this.cargando = false;
      }

    });
  }

  anularVenta(venta: any): void {

    if (venta.estado === 'ANULADA') {
      this.mensaje = 'Esta venta ya se encuentra anulada.';
      return;
    }

    const confirmar = confirm(
      `¿Está seguro de anular la venta por Q ${venta.total}?`
    );

    if (!confirmar) {
      return;
    }

    this.http.patch(
      `${this.apiUrl}/${venta.id}/anular`,
      {},
      {
        headers: this.obtenerHeaders()
      }
    ).subscribe({

      next: () => {

        this.mensaje =
          'Venta anulada correctamente.';

        this.cargarVentas();
      },

      error: (error) => {

        if (error.status === 409) {
          this.mensaje =
            'La venta ya se encuentra anulada.';
          return;
        }

        if (error.status === 403) {
          this.mensaje =
            'No tienes permisos para anular ventas.';
          return;
        }

        this.mensaje =
          error.error?.mensaje ||
          'No se pudo anular la venta.';
      }

    });
  }
}