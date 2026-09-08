import {
  Component,
  OnInit
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

import { environment } from '../../../enviroments/enviromet';

@Component({
  selector: 'app-menu-diario',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './menu-diario.component.html',
  styleUrl: './menu-diario.component.scss'
})
export class MenuDiarioComponent implements OnInit {

  productos: any[] = [];

  productosSeleccionados: string[] = [];

  productoAgregarId = '';

  menuActual: any = null;

  fecha = this.obtenerFechaLocal();

  mensaje = '';

  cargando = false;

  private productosUrl =
    `${environment.apiUrl}/productos`;

  private menusUrl =
    `${environment.apiUrl}/menus`;

  constructor(
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.cargarMenuHoy();
    this.cargarProductos();
  }

  get productosDisponibles(): number {

    if (!this.menuActual) {
      return 0;
    }

    return this.menuActual.productos.filter(
      (producto: any) =>
        producto.disponible
    ).length;
  }

  get productosAgotados(): number {

    if (!this.menuActual) {
      return 0;
    }

    return this.menuActual.productos.filter(
      (producto: any) =>
        !producto.disponible
    ).length;
  }

  obtenerFechaLocal(): string {

    const hoy = new Date();

    const anio =
      hoy.getFullYear();

    const mes =
      String(
        hoy.getMonth() + 1
      ).padStart(2, '0');

    const dia =
      String(
        hoy.getDate()
      ).padStart(2, '0');

    return `${anio}-${mes}-${dia}`;
  }

  get productosFueraDelMenu(): any[] {

    if (!this.menuActual) {
      return this.productos;
    }

    const productosMenu =
      new Set(
        this.menuActual.productos.map(
          (producto: any) =>
            producto.productoId
        )
      );

    return this.productos.filter(
      producto =>
        producto.activo &&
        !productosMenu.has(producto.id)
    );
  }

  estaSeleccionado(
    productoId: string
  ): boolean {

    return this.productosSeleccionados
      .includes(productoId);
  }

  cargarMenuHoy(): void {

    this.http.get<any>(
      `${this.menusUrl}/hoy`
    ).subscribe({

      next: (menu) => {

        this.menuActual =
          menu;
      },

      error: (error) => {

        if (error.status === 404) {

          this.menuActual = null;

          return;
        }

        this.mensaje =
          'No se pudo consultar el menú del día.';
      }

    });
  }

  cargarProductos(): void {

    this.http.get<any[]>(
      this.productosUrl
    ).subscribe({

      next: (productos) => {

        this.productos =
          productos.filter(
            producto =>
              producto.activo
          );
      },

      error: () => {

        this.mensaje =
          'No se pudieron cargar los productos.';
      }

    });
  }

  cambiarSeleccion(
    productoId: string,
    seleccionado: boolean
  ): void {

    if (seleccionado) {

      if (
        !this.productosSeleccionados
          .includes(productoId)
      ) {

        this.productosSeleccionados
          .push(productoId);
      }

    } else {

      this.productosSeleccionados =
        this.productosSeleccionados.filter(
          id =>
            id !== productoId
        );
    }
  }

  crearMenu(): void {

    if (
      this.productosSeleccionados
        .length === 0
    ) {

      this.mensaje =
        'Selecciona al menos un producto.';

      return;
    }

    this.cargando = true;

    this.mensaje = '';

    const datos = {

      fecha:
        this.fecha,

      productos:
        this.productosSeleccionados.map(
          id => ({
            productoId: id
          })
        )
    };

    this.http.post(
      this.menusUrl,
      datos
    ).subscribe({

      next: () => {

        this.cargando =
          false;

        this.mensaje =
          'Menú diario creado correctamente.';

        this.productosSeleccionados =
          [];

        this.cargarMenuHoy();
      },

      error: (error) => {

        this.cargando =
          false;

        if (
          error.status === 409
        ) {

          this.mensaje =
            'Ya existe un menú para esta fecha.';

          return;
        }

        this.mensaje =
          error.error?.mensaje ||
          'No se pudo crear el menú.';
      }

    });
  }

  cambiarDisponibilidad(
    producto: any
  ): void {

    if (!this.menuActual) {

      this.mensaje =
        'No existe un menú activo.';

      return;
    }

    const nuevaDisponibilidad =
      !producto.disponible;

    const url =
      `${this.menusUrl}/${this.menuActual.id}` +
      `/productos/${producto.productoId}` +
      `/disponibilidad?disponible=${nuevaDisponibilidad}`;

    this.http.patch(
      url,
      {}
    ).subscribe({

      next: () => {

        producto.disponible =
          nuevaDisponibilidad;

        this.mensaje =
          nuevaDisponibilidad
            ? 'Producto disponible nuevamente.'
            : 'Producto marcado como agotado.';
      },

      error: (error) => {

        this.mensaje =
          error.error?.mensaje ||
          'No se pudo cambiar la disponibilidad.';
      }

    });
  }

  agregarProductoAlMenu(): void {

    if (!this.menuActual) {

      this.mensaje =
        'No existe un menú activo.';

      return;
    }

    if (!this.productoAgregarId) {

      this.mensaje =
        'Selecciona un producto.';

      return;
    }

    this.http.post(
      `${this.menusUrl}/${this.menuActual.id}/productos/${this.productoAgregarId}`,
      {}
    ).subscribe({

      next: () => {

        this.mensaje =
          'Producto agregado al menú correctamente.';

        this.productoAgregarId =
          '';

        this.cargarMenuHoy();
      },

      error: (error) => {

        this.mensaje =
          error.error?.mensaje ||
          'No se pudo agregar el producto al menú.';
      }

    });
  }

  quitarProductoDelMenu(
    producto: any
  ): void {

    if (!this.menuActual) {

      this.mensaje =
        'No existe un menú activo.';

      return;
    }

    const confirmar =
      confirm(
        `¿Deseas quitar "${producto.nombre}" del menú de hoy?`
      );

    if (!confirmar) {
      return;
    }

    this.http.delete(
      `${this.menusUrl}/${this.menuActual.id}/productos/${producto.productoId}`
    ).subscribe({

      next: () => {

        this.mensaje =
          'Producto quitado del menú correctamente.';

        this.cargarMenuHoy();
      },

      error: (error) => {

        if (
          error.status === 409
        ) {

          this.mensaje =
            error.error?.mensaje ||
            'Este producto ya tiene ventas y no puede quitarse.';

          return;
        }

        this.mensaje =
          error.error?.mensaje ||
          'No se pudo quitar el producto del menú.';
      }

    });
  }
}