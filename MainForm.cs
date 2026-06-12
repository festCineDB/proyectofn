using System.Data;
using Microsoft.Data.SqlClient;

namespace FestCine.Cliente;

/// <summary>
/// Ventana principal del sistema FestCine.
///   Módulo 1 (Cajero):        pestaña Taquilla  → invoca SP ComprarEntrada (P1).
///   Módulo 2 (Administrador): pestaña Agenda    → invoca SP ProgramarProyeccion;
///                             el Trigger TR_ControlAgenda valida el cruce de horarios.
///   Extra:                    pestaña Abonos    → invoca SP VenderAbono (T1, COMMIT/ROLLBACK).
///   Reportes (Fase 3):        Ranking, Premiación y Financiero vía SPs de reporte.
/// Toda la interacción con la BD pasa por ProcedimientosBD (sin SQL embebido).
/// </summary>
public class MainForm : Form
{
    /* ── Módulo 1: Taquilla ── */
    private readonly ComboBox cboAsistente  = NuevoCombo();
    private readonly ComboBox cboPelicula   = NuevoCombo();
    private readonly ComboBox cboProyeccion = NuevoCombo();
    private readonly ComboBox cboTarifa     = NuevoCombo();
    private readonly DataGridView gridTaquilla = NuevaGrilla();

    /* ── Módulo 2: Agenda ── */
    private readonly ComboBox cboPeliculaAg = NuevoCombo();
    private readonly ComboBox cboSala       = NuevoCombo();
    private readonly DateTimePicker dtpFechaHora = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy HH:mm",
        ShowUpDown = true,
        Width = 180
    };
    private readonly CheckBox chkQA = new() { Text = "Incluye sesión Q&A", AutoSize = true };
    private readonly DataGridView gridAgenda = NuevaGrilla();

    /* ── Abonos (T1) ── */
    private readonly ComboBox cboAsistenteAb = NuevoCombo();
    private readonly ComboBox cboTipoAbono   = NuevoCombo();
    private readonly DataGridView gridAbonos = NuevaGrilla();

    /* ── Reportes ── */
    private readonly DataGridView gridRanking    = NuevaGrilla();
    private readonly DataGridView gridPremiacion = NuevaGrilla();
    private readonly DataGridView gridFinanciero = NuevaGrilla();
    private readonly Label lblRanking    = NuevaEtiquetaEstado();
    private readonly Label lblPremiacion = NuevaEtiquetaEstado();
    private readonly Label lblFinanciero = NuevaEtiquetaEstado();

    public MainForm()
    {
        Text = "FestCine 2026 — Sistema de Gestión (Cliente C# / SQL Server)";
        Width = 1080;
        Height = 680;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CrearTabTaquilla());
        tabs.TabPages.Add(CrearTabAgenda());
        tabs.TabPages.Add(CrearTabAbonos());
        tabs.TabPages.Add(CrearTabReporte("🏆 Ranking", gridRanking, lblRanking, CargarRanking));
        tabs.TabPages.Add(CrearTabReporte("🎖 Premiación", gridPremiacion, lblPremiacion, CargarPremiacion));
        tabs.TabPages.Add(CrearTabReporte("💰 Financiero", gridFinanciero, lblFinanciero, CargarFinanciero));
        Controls.Add(tabs);

        Load += (_, _) => CargarDatosIniciales();
    }

    /* ════════════════ Construcción de pestañas ════════════════ */

    private TabPage CrearTabTaquilla()
    {
        var tab = new TabPage("🎟 Taquilla (Cajero)");

        var panel = NuevoPanelSuperior(150);
        panel.Controls.Add(Etiquetado("Asistente:", cboAsistente));
        panel.Controls.Add(Etiquetado("Película:", cboPelicula));
        panel.Controls.Add(Etiquetado("Proyección:", cboProyeccion));
        panel.Controls.Add(Etiquetado("Tarifa:", cboTarifa));

        var btnComprar = NuevoBoton("🎟 Confirmar Compra");
        btnComprar.Click += (_, _) => ComprarEntrada();
        panel.Controls.Add(btnComprar);

        cboPelicula.SelectedIndexChanged += (_, _) => CargarProyeccionesCombo();

        tab.Controls.Add(gridTaquilla);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CrearTabAgenda()
    {
        var tab = new TabPage("📅 Agenda (Administrador)");

        var panel = NuevoPanelSuperior(150);
        panel.Controls.Add(Etiquetado("Película:", cboPeliculaAg));
        panel.Controls.Add(Etiquetado("Sala:", cboSala));
        panel.Controls.Add(Etiquetado("Fecha y hora:", dtpFechaHora));
        panel.Controls.Add(Etiquetado(" ", chkQA));

        var btnProgramar = NuevoBoton("📅 Programar Proyección");
        btnProgramar.Click += (_, _) => ProgramarProyeccion();
        panel.Controls.Add(btnProgramar);

        tab.Controls.Add(gridAgenda);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CrearTabAbonos()
    {
        var tab = new TabPage("🪪 Abonos (Transacción T1)");

        var panel = NuevoPanelSuperior(150);
        panel.Controls.Add(Etiquetado("Asistente:", cboAsistenteAb));
        panel.Controls.Add(Etiquetado("Tipo de abono:", cboTipoAbono));

        var btnVender = NuevoBoton("🪪 Vender Abono (COMMIT)");
        btnVender.Click += (_, _) => VenderAbono(pagoExitoso: true);
        panel.Controls.Add(btnVender);

        var btnFallo = NuevoBoton("⚡ Simular Fallo de Pasarela (ROLLBACK)");
        btnFallo.BackColor = Color.DarkOrange;
        btnFallo.Click += (_, _) => VenderAbono(pagoExitoso: false);
        panel.Controls.Add(btnFallo);

        tab.Controls.Add(gridAbonos);
        tab.Controls.Add(panel);
        return tab;
    }

    private static TabPage CrearTabReporte(string titulo, DataGridView grid, Label lblEstado, Action cargar)
    {
        var tab = new TabPage(titulo);

        var panel = NuevoPanelSuperior(70);
        var btn = NuevoBoton("↺ Actualizar reporte");
        btn.Click += (_, _) => cargar();
        panel.Controls.Add(btn);
        panel.Controls.Add(lblEstado);

        tab.Controls.Add(grid);
        tab.Controls.Add(panel);
        return tab;
    }

    /* ════════════════ Carga de datos (vía SPs) ════════════════ */

    private void CargarDatosIniciales()
    {
        Intentar(() =>
        {
            DataTable asistentes = ProcedimientosBD.ListarAsistentes();
            EnlazarCombo(cboAsistente,   asistentes.Copy(), "Nombre", "IdAsistente");
            EnlazarCombo(cboAsistenteAb, asistentes.Copy(), "Nombre", "IdAsistente");

            DataTable peliculas = ProcedimientosBD.ListarPeliculas();
            EnlazarCombo(cboPelicula,   peliculas.Copy(), "Titulo", "IdPelicula");
            EnlazarCombo(cboPeliculaAg, peliculas.Copy(), "Titulo", "IdPelicula");

            DataTable tarifas = ProcedimientosBD.ListarTarifas();
            AgregarColumnaDescriptiva(tarifas, r => $"{r["NombreTarifa"]} — Bs. {r["Precio"]}");
            EnlazarCombo(cboTarifa, tarifas, "Descripcion_UI", "IdTarifa");

            DataTable salas = ProcedimientosBD.ListarSalas();
            AgregarColumnaDescriptiva(salas, r => $"{r["NombreSala"]} — {r["NombreSede"]} (Cap. {r["Capacidad"]})");
            EnlazarCombo(cboSala, salas, "Descripcion_UI", "IdSala");

            DataTable tiposAbono = ProcedimientosBD.ListarTiposAbono();
            AgregarColumnaDescriptiva(tiposAbono, r => $"{r["NombreAbono"]} — Bs. {r["Precio"]}");
            EnlazarCombo(cboTipoAbono, tiposAbono, "Descripcion_UI", "IdTipoAbono");

            dtpFechaHora.Value = DateTime.Today.AddDays(1).AddHours(19);

            CargarProyeccionesCombo();
            RefrescarGrillasProyecciones();
            CargarAbonosGrilla();
        });
    }

    private void CargarProyeccionesCombo()
    {
        if (cboPelicula.SelectedValue is not int idPelicula) return;
        Intentar(() =>
        {
            DataTable proyecciones = ProcedimientosBD.ListarProyecciones(idPelicula);
            AgregarColumnaDescriptiva(proyecciones, r =>
                $"{r["NombreSala"]} — {((DateTime)r["FechaHora"]):dd/MM/yyyy HH:mm} (Aforo: {r["AforoDisponible"]})");
            EnlazarCombo(cboProyeccion, proyecciones, "Descripcion_UI", "IdProyeccion");
        });
    }

    private void RefrescarGrillasProyecciones()
    {
        Intentar(() =>
        {
            gridTaquilla.DataSource = ProcedimientosBD.ListarProyecciones();
            gridAgenda.DataSource   = ProcedimientosBD.ListarProyecciones();
        });
    }

    private void CargarAbonosGrilla() =>
        Intentar(() => gridAbonos.DataSource = ProcedimientosBD.ListarAbonosVendidos());

    private void CargarRanking() =>
        Intentar(() =>
        {
            (DataTable datos, string respuesta) = ProcedimientosBD.ReporteRanking();
            gridRanking.DataSource = datos;
            lblRanking.Text = respuesta;
        });

    private void CargarPremiacion() =>
        Intentar(() =>
        {
            (DataTable datos, string respuesta) = ProcedimientosBD.ReportePremiacion();
            gridPremiacion.DataSource = datos;
            lblPremiacion.Text = respuesta;
        });

    private void CargarFinanciero() =>
        Intentar(() =>
        {
            (DataTable datos, string respuesta) = ProcedimientosBD.ReporteFinanciero();
            gridFinanciero.DataSource = datos;
            lblFinanciero.Text = respuesta;
        });

    /* ════════════════ Operaciones de negocio (vía SPs) ════════════════ */

    private void ComprarEntrada()
    {
        if (cboAsistente.SelectedValue is not int idAsistente ||
            cboProyeccion.SelectedValue is not int idProyeccion ||
            cboTarifa.SelectedValue is not int idTarifa)
        {
            Aviso("Complete todos los campos antes de confirmar la compra.");
            return;
        }

        Intentar(() =>
        {
            // Módulo 1: invoca el Procedimiento Almacenado P1 (ComprarEntrada)
            string respuesta = ProcedimientosBD.ComprarEntrada(idAsistente, idProyeccion, idTarifa);
            Exito(respuesta);
            CargarProyeccionesCombo();
            RefrescarGrillasProyecciones();
        });
    }

    private void ProgramarProyeccion()
    {
        if (cboPeliculaAg.SelectedValue is not int idPelicula ||
            cboSala.SelectedValue is not int idSala)
        {
            Aviso("Seleccione la película y la sala.");
            return;
        }

        Intentar(() =>
        {
            // Módulo 2: el INSERT lo hace el SP; el Trigger TR_ControlAgenda
            // rechaza la inserción si existe un cruce de horarios.
            string respuesta = ProcedimientosBD.ProgramarProyeccion(
                idPelicula, idSala, dtpFechaHora.Value, chkQA.Checked);
            Exito(respuesta);
            RefrescarGrillasProyecciones();
        });
    }

    private void VenderAbono(bool pagoExitoso)
    {
        if (cboAsistenteAb.SelectedValue is not int idAsistente ||
            cboTipoAbono.SelectedValue is not int idTipoAbono)
        {
            Aviso("Seleccione el asistente y el tipo de abono.");
            return;
        }

        Intentar(() =>
        {
            // T1: transacción atómica en el servidor (COMMIT / ROLLBACK)
            string respuesta = ProcedimientosBD.VenderAbono(idAsistente, idTipoAbono, pagoExitoso);
            Exito(respuesta);
            CargarAbonosGrilla();
        });
    }

    /* ════════════════ Manejo de excepciones ════════════════
       La aplicación atrapa los errores lanzados por el servidor
       (THROW de los SPs o del Trigger TR1) y muestra un mensaje
       amigable en lugar de colapsar con el error crudo de SQL. */

    private void Intentar(Action accion)
    {
        try
        {
            accion();
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            // Error de negocio lanzado por un SP o por el Trigger TR1
            MessageBox.Show(this, ex.Message, "FestCine — Operación rechazada",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (SqlException ex)
        {
            // Error técnico de la BD (integridad, conexión, etc.)
            MessageBox.Show(this,
                "No fue posible completar la operación.\n\nDetalle: " + ex.Message,
                "FestCine — Error de base de datos",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Ocurrió un error inesperado.\n\nDetalle: " + ex.Message,
                "FestCine — Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Exito(string mensaje) =>
        MessageBox.Show(this, mensaje, "FestCine — Operación exitosa",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void Aviso(string mensaje) =>
        MessageBox.Show(this, mensaje, "FestCine — Datos incompletos",
            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

    /* ════════════════ Auxiliares de interfaz ════════════════ */

    private static ComboBox NuevoCombo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 280
    };

    private static DataGridView NuevaGrilla() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false,
        BackgroundColor = Color.White
    };

    private static Label NuevaEtiquetaEstado() => new()
    {
        AutoSize = true,
        ForeColor = Color.DarkGreen,
        Margin = new Padding(10, 12, 0, 0)
    };

    private static FlowLayoutPanel NuevoPanelSuperior(int alto) => new()
    {
        Dock = DockStyle.Top,
        Height = alto,
        Padding = new Padding(10),
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = true
    };

    private static Button NuevoBoton(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        Padding = new Padding(8, 4, 8, 4),
        Margin = new Padding(10, 18, 0, 0),
        BackColor = Color.FromArgb(200, 30, 45),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat
    };

    private static Panel Etiquetado(string texto, Control control)
    {
        var contenedor = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Margin = new Padding(0, 0, 12, 0)
        };
        contenedor.Controls.Add(new Label { Text = texto, AutoSize = true });
        contenedor.Controls.Add(control);
        return contenedor;
    }

    private static void EnlazarCombo(ComboBox combo, DataTable datos, string display, string valor)
    {
        // DisplayMember/ValueMember se asignan ANTES que DataSource para que
        // los eventos SelectedIndexChanged no se disparen con datos a medias.
        combo.DisplayMember = display;
        combo.ValueMember = valor;
        combo.DataSource = datos;
    }

    /// <summary>Agrega a la tabla una columna de texto descriptivo para mostrar en combos.</summary>
    private static void AgregarColumnaDescriptiva(DataTable dt, Func<DataRow, string> formato)
    {
        dt.Columns.Add("Descripcion_UI", typeof(string));
        foreach (DataRow fila in dt.Rows)
            fila["Descripcion_UI"] = formato(fila);
    }
}
