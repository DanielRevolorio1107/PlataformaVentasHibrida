import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-menu-diario',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './menu-diario.component.html',
  styleUrl: './menu-diario.component.scss'
})
export class MenuDiarioComponent implements OnInit {

  productos: any[] = [];
  productosSeleccionados: string[] = [];

  menuActual: any = null;

  fecha = this.obtenerFechaLocal();

  mensaje = '';
  cargando = false;

  private productosUrl = 'http://localhost:5080/api/productos';
  private menusUrl = 'http://localhost:5080/api/menus';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.cargarMenuHoy();
  }

  obtenerFechaLocal(): string {
    const hoy = new Date();

    const anio = hoy.getFullYear();
    const mes = String(hoy.getMonth() + 1).padStart(2, '0');
    const dia = String(hoy.getDate()).padStart(2, '0');

    return `${anio}-${mes}-${dia}`;
  }

  obtenerHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');

    return new HttpHeaders({
      Authorization: `Bearer ${token}`
    });
  }

  cargarMenuHoy(): void {
    this.http.get<any>(
      `${this.menusUrl}/hoy`,
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: (menu) => {
        this.menuActual = menu;
      },
      error: (error) => {
        if (error.status === 404) {
          this.menuActual = null;
          this.cargarProductos();
          return;
        }

        this.mensaje = 'No se pudo consultar el menú del día.';
      }
    });
  }

  cargarProductos(): void {
    this.http.get<any[]>(
      this.productosUrl,
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: (productos) => {
        this.productos = productos.filter(p => p.activo);
      },
      error: () => {
        this.mensaje = 'No se pudieron cargar los productos.';
      }
    });
  }

  cambiarSeleccion(productoId: string, seleccionado: boolean): void {
    if (seleccionado) {
      if (!this.productosSeleccionados.includes(productoId)) {
        this.productosSeleccionados.push(productoId);
      }
    } else {
      this.productosSeleccionados =
        this.productosSeleccionados.filter(id => id !== productoId);
    }
  }

  crearMenu(): void {

    if (this.productosSeleccionados.length === 0) {
      this.mensaje = 'Selecciona al menos un producto.';
      return;
    }

    this.cargando = true;
    this.mensaje = '';

    const datos = {
      fecha: this.fecha,
      productos: this.productosSeleccionados.map(id => ({
        productoId: id
      }))
    };

    this.http.post(
      this.menusUrl,
      datos,
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: () => {
        this.cargando = false;
        this.mensaje = 'Menú diario creado correctamente.';
        this.cargarMenuHoy();
      },
      error: (error) => {
        this.cargando = false;

        if (error.status === 409) {
          this.mensaje = 'Ya existe un menú para esta fecha.';
          return;
        }

        this.mensaje = 'No se pudo crear el menú.';
      }
    });
  }

  cambiarDisponibilidad(producto: any): void {

    const nuevaDisponibilidad = !producto.disponible;

    const url =
      `${this.menusUrl}/${this.menuActual.id}` +
      `/productos/${producto.productoId}` +
      `/disponibilidad?disponible=${nuevaDisponibilidad}`;

    this.http.patch(
      url,
      {},
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: () => {
        producto.disponible = nuevaDisponibilidad;

        this.mensaje = nuevaDisponibilidad
          ? 'Producto disponible nuevamente.'
          : 'Producto marcado como agotado.';
      },
      error: () => {
        this.mensaje = 'No se pudo cambiar la disponibilidad.';
      }
    });
  }
}