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

    private readonly TextBox txtTituloPeli = new() { Width = 160 };
    private readonly NumericUpDown nudAnioPeli = new() { Width = 80, Maximum = 2100, Minimum = 1888 };
    private readonly NumericUpDown nudDuracionPeli = new() { Width = 60, Maximum = 600, Minimum = 1 };
    private readonly TextBox txtPaisPeli = new() { Width = 100 };
    private readonly TextBox txtSinopsisPeli = new() { Width = 200 };
    private readonly ComboBox cboClasifPeli = NuevoCombo();
    private readonly ComboBox cboFormatoPeli = NuevoCombo();
    private readonly ComboBox cboEstadoPeli = NuevoCombo();
    private readonly CheckedListBox chkGenerosPeli = new() { Height = 90, Width = 200, CheckOnClick = true };

    private readonly TextBox txtNombreSala = new() { Width = 140 };
    private readonly NumericUpDown nudCapacidadSala = new() { Width = 80, Maximum = 9999, Minimum = 1 };
    private readonly ComboBox cboSedeSala = NuevoCombo();

    private readonly TextBox txtNombreAsist = new() { Width = 160 };
    private readonly TextBox txtEmailAsist = new() { Width = 160 };
    private readonly TextBox txtTelAsist = new() { Width = 110 };
    private readonly ComboBox cboTipoAsist = NuevoCombo();
    private readonly DataGridView gridAsistentes = NuevaGrilla();

    /* ── Abonos (T1) ── */
    private readonly ComboBox cboAsistenteAb = NuevoCombo();
    private readonly ComboBox cboTipoAbono   = NuevoCombo();
    private readonly DataGridView gridAbonos = NuevaGrilla();

    /* ── Eventos Paralelos ── */
    private readonly ComboBox cboEvento       = NuevoCombo();
    private readonly DataGridView gridEventos = NuevaGrilla();
    private readonly DataGridView gridExpositores = NuevaGrilla();
    private readonly DataGridView gridAsistentesEv = NuevaGrilla();
    private readonly ComboBox cboNuevoExpositor = NuevoCombo();

    /* ── Logística y Patrocinios ── */
    private readonly ComboBox cboInvitadoAloj   = NuevoCombo();
    private readonly ComboBox cboHotel           = NuevoCombo();
    private readonly DateTimePicker dtpCheckIn   = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly DateTimePicker dtpCheckOut  = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly ComboBox cboHabitacion      = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
    private readonly DataGridView gridAlojamientos = NuevaGrilla();
    private readonly ComboBox cboInvitadoTras    = NuevoCombo();
    private readonly ComboBox cboTipoTraslado    = new() { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cboOrigen          = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 120 };
    private readonly ComboBox cboDestino         = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 120 };
    private readonly DateTimePicker dtpFechaTras = new() { Format = DateTimePickerFormat.Short, Width = 100 };
    private readonly ComboBox cboVuelo           = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 100 };
    private readonly DataGridView gridTraslados  = NuevaGrilla();
    private readonly ComboBox cboPatrocinador    = NuevoCombo();
    private readonly ComboBox cboEdicionPat      = NuevoCombo();
    private readonly ComboBox cboTipoAporte      = new() { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown nudMonto       = new() { Width = 100, DecimalPlaces = 2, Maximum = 999999, Minimum = 1, Value = 1 };
    private readonly TextBox txtDescAporte       = new() { Width = 250 };
    private readonly DataGridView gridPatrocinios = NuevaGrilla();

    /* ── Competencia y Jurados ── */
    private readonly ComboBox cboCategoria    = NuevoCombo();
    private readonly ComboBox cboMiembroEval  = NuevoCombo();
    private readonly ComboBox cboPeliculaEval = NuevoCombo();
    private readonly ComboBox cboPeliculaPrem = NuevoCombo();
    private readonly ComboBox cboCategoriaPrem = NuevoCombo();
    private readonly NumericUpDown nudPuntuacion = new() { Minimum = 1, Maximum = 10, Value = 5, Width = 80 };
    private readonly TextBox txtComentario = new() { Width = 300, Height = 60, Multiline = true };
    private readonly DataGridView gridEval = NuevaGrilla();
    private readonly DataGridView gridPremios = NuevaGrilla();
    private readonly DataGridView gridMiembrosCat = NuevaGrilla();

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
        BackColor = Color.FromArgb(235, 235, 240);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(10, 6) };
        tabs.TabPages.Add(CrearTabTaquilla());
        tabs.TabPages.Add(CrearTabAgenda());
        tabs.TabPages.Add(CrearTabAdminPeliculasSalas());
        tabs.TabPages.Add(CrearTabAbonos());
        tabs.TabPages.Add(CrearTabEventos());
        tabs.TabPages.Add(CrearTabLogistica());
        tabs.TabPages.Add(CrearTabCompetencia());
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

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        panel.Controls.Add(Etiquetado("Película:", cboPeliculaAg));
        panel.Controls.Add(Etiquetado("Sala:", cboSala));
        panel.Controls.Add(Etiquetado("Fecha y hora:", dtpFechaHora));
        panel.Controls.Add(Etiquetado(" ", chkQA));
        var btnProgramar = NuevoBoton("📅 Programar Proyección");
        btnProgramar.Click += (_, _) => ProgramarProyeccion();
        panel.Controls.Add(btnProgramar);

        var panelAsist = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.FromArgb(248, 248, 252)
        };
        panelAsist.Controls.Add(CrearLabel("╶  REGISTRAR ASISTENTE  ╴", 0, 6));
        panelAsist.Controls.Add(Etiquetado("Nombre:", txtNombreAsist));
        panelAsist.Controls.Add(Etiquetado("Email:", txtEmailAsist));
        panelAsist.Controls.Add(Etiquetado("Teléfono:", txtTelAsist));
        cboTipoAsist.Items.AddRange(new[] { "General", "Prensa", "Industria", "VIP", "Jurado" });
        cboTipoAsist.Width = 100;
        cboTipoAsist.SelectedIndex = 0;
        panelAsist.Controls.Add(Etiquetado("Tipo:", cboTipoAsist));
        var btnAsist = NuevoBoton("➕ Asistente");
        btnAsist.Click += (_, _) => RegistrarAsistente();
        panelAsist.Controls.Add(btnAsist);

        gridAgenda.Dock = DockStyle.Fill;
        gridAsistentes.Dock = DockStyle.Fill;
        panel.Dock = DockStyle.Fill;
        panelAsist.Dock = DockStyle.Fill;

        var tl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.FromArgb(235, 235, 240)
        };
        tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tl.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tl.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        tl.Controls.Add(panel, 0, 0);
        tl.Controls.Add(gridAgenda, 0, 1);
        tl.Controls.Add(panelAsist, 0, 2);
        tl.Controls.Add(gridAsistentes, 0, 3);

        tab.Controls.Add(tl);
        return tab;
    }

    private TabPage CrearTabAdminPeliculasSalas()
    {
        var tab = new TabPage("🎬 Admin Películas / Salas");

        var panelCrear = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        panelCrear.Controls.Add(CrearLabel("╶  CREAR PELÍCULA  ╴", 0, 6));
        panelCrear.Controls.Add(Etiquetado("Título:", txtTituloPeli));
        panelCrear.Controls.Add(Etiquetado("Año:", nudAnioPeli));
        panelCrear.Controls.Add(Etiquetado("Dur.(min):", nudDuracionPeli));
        panelCrear.Controls.Add(Etiquetado("País:", txtPaisPeli));
        panelCrear.Controls.Add(Etiquetado("Sinopsis:", txtSinopsisPeli));
        cboClasifPeli.Items.AddRange(new[] { "G", "PG", "PG-13", "R", "NC-17" });
        cboClasifPeli.Width = 80;
        panelCrear.Controls.Add(Etiquetado("Clasif:", cboClasifPeli));
        cboFormatoPeli.Items.AddRange(new[] { "2D", "3D", "IMAX", "4DX" });
        cboFormatoPeli.Width = 80;
        panelCrear.Controls.Add(Etiquetado("Formato:", cboFormatoPeli));
        cboEstadoPeli.Items.AddRange(new[] { "Postulada", "Seleccionada", "Rechazada", "Premiada" });
        cboEstadoPeli.Width = 110;
        panelCrear.Controls.Add(Etiquetado("Estado:", cboEstadoPeli));
        var contGeneros = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(15, 0, 20, 0) };
        contGeneros.Controls.Add(new Label { Text = "Géneros:", AutoSize = true });
        contGeneros.Controls.Add(chkGenerosPeli);
        panelCrear.Controls.Add(contGeneros);
        var btnCrearPeli = NuevoBoton("➕ Película");
        btnCrearPeli.Click += (_, _) => CrearPelicula();
        panelCrear.Controls.Add(btnCrearPeli);

        cboClasifPeli.SelectedIndex = 0;
        cboFormatoPeli.SelectedIndex = 0;
        cboEstadoPeli.SelectedIndex = 0;

        var panelCrearSala = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        panelCrearSala.Controls.Add(CrearLabel("╶  CREAR SALA  ╴", 0, 6));
        panelCrearSala.Controls.Add(Etiquetado("Nombre:", txtNombreSala));
        panelCrearSala.Controls.Add(Etiquetado("Capacidad:", nudCapacidadSala));
        panelCrearSala.Controls.Add(Etiquetado("Sede:", cboSedeSala));
        var btnCrearSala = NuevoBoton("➕ Sala");
        btnCrearSala.Click += (_, _) => CrearSala();
        panelCrearSala.Controls.Add(btnCrearSala);

        tab.Controls.Add(panelCrearSala);
        tab.Controls.Add(panelCrear);
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
        btnFallo.BackColor = Color.FromArgb(200, 120, 30);
        btnFallo.FlatAppearance.MouseOverBackColor = Color.FromArgb(170, 100, 20);
        btnFallo.FlatAppearance.MouseDownBackColor = Color.FromArgb(140, 80, 15);
        btnFallo.Click += (_, _) => VenderAbono(pagoExitoso: false);
        panel.Controls.Add(btnFallo);

        tab.Controls.Add(gridAbonos);
        tab.Controls.Add(panel);
        return tab;
    }

    private TabPage CrearTabLogistica()
    {
        var tab = new TabPage("🏨 Logística y Patrocinios");
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12) };

        cboHabitacion.Items.AddRange(new[] { "101","102","103","104","201","202","203","204","301","302","303","304","401","402","403","404","501","502" });
        cboTipoTraslado.Items.AddRange(new[] { "Vuelo", "Transfer", "Taxi" });
        cboOrigen.Items.AddRange(new[] { "Santa Cruz (VVI)","La Paz (LPB)","Cochabamba (CBB)","Buenos Aires (EZE)","Lima (LIM)","Santiago (SCL)","Madrid (MAD)","Miami (MIA)" });
        cboDestino.Items.AddRange(new[] { "Santa Cruz (VVI)","La Paz (LPB)","Cochabamba (CBB)","Buenos Aires (EZE)","Lima (LIM)","Santiago (SCL)","Madrid (MAD)","Miami (MIA)" });
        cboVuelo.Items.AddRange(new[] { "OB101","LA832","LA2081","AM543","CM216","AA900","IB6780","AV125" });
        cboTipoAporte.Items.AddRange(new[] { "Economico", "Especie" });
        cboTipoAporte.SelectedIndexChanged += (_, _) => nudMonto.Enabled = cboTipoAporte.Text == "Economico";

        var lblAloj = new Label { Text = "══  ALOJAMIENTOS  ══", AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DarkBlue, Margin = new Padding(0, 6, 0, 4) };
        flow.Controls.Add(lblAloj);

        var pAloj = new Panel { Height = 70, Width = 900 };
        pAloj.Controls.Add(CrearLabel("Invitado:", 0, 12));
        cboInvitadoAloj.Location = new Point(65, 9); cboInvitadoAloj.Width = 200; pAloj.Controls.Add(cboInvitadoAloj);
        pAloj.Controls.Add(CrearLabel("Hotel:", 285, 12));
        cboHotel.Location = new Point(330, 9); cboHotel.Width = 200; pAloj.Controls.Add(cboHotel);
        pAloj.Controls.Add(CrearLabel("Habitación:", 550, 12));
        cboHabitacion.Location = new Point(630, 9); cboHabitacion.Width = 100; pAloj.Controls.Add(cboHabitacion);
        pAloj.Controls.Add(CrearLabel("Check-in:", 0, 40));
        dtpCheckIn.Location = new Point(65, 37); pAloj.Controls.Add(dtpCheckIn);
        pAloj.Controls.Add(CrearLabel("Check-out:", 200, 40));
        dtpCheckOut.Location = new Point(275, 37); pAloj.Controls.Add(dtpCheckOut);
        var btnAloj = NuevoBoton("➕ Alojar"); btnAloj.Location = new Point(440, 36); btnAloj.Width = 90; btnAloj.Height = 28;
        btnAloj.Click += (_, _) => AgregarAlojamiento();
        pAloj.Controls.Add(btnAloj);
        flow.Controls.Add(pAloj);

        gridAlojamientos.Height = 100; gridAlojamientos.Width = 1160;
        flow.Controls.Add(gridAlojamientos);

        var lblTras = new Label { Text = "══  TRASLADOS  ══", AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DarkBlue, Margin = new Padding(0, 8, 0, 4) };
        flow.Controls.Add(lblTras);

        var pTras = new Panel { Height = 70, Width = 900 };
        pTras.Controls.Add(CrearLabel("Invitado:", 0, 12));
        cboInvitadoTras.Location = new Point(65, 9); cboInvitadoTras.Width = 200; pTras.Controls.Add(cboInvitadoTras);
        pTras.Controls.Add(CrearLabel("Tipo:", 285, 12));
        cboTipoTraslado.Location = new Point(320, 9); cboTipoTraslado.Width = 90; pTras.Controls.Add(cboTipoTraslado);
        pTras.Controls.Add(CrearLabel("Origen:", 430, 12));
        cboOrigen.Location = new Point(485, 9); cboOrigen.Width = 140; pTras.Controls.Add(cboOrigen);
        pTras.Controls.Add(CrearLabel("Destino:", 645, 12));
        cboDestino.Location = new Point(700, 9); cboDestino.Width = 140; pTras.Controls.Add(cboDestino);
        pTras.Controls.Add(CrearLabel("Fecha:", 0, 40));
        dtpFechaTras.Location = new Point(50, 37); pTras.Controls.Add(dtpFechaTras);
        pTras.Controls.Add(CrearLabel("Vuelo:", 200, 40));
        cboVuelo.Location = new Point(245, 37); cboVuelo.Width = 100; pTras.Controls.Add(cboVuelo);
        var btnTras = NuevoBoton("➕ Trasladar"); btnTras.Location = new Point(380, 36); btnTras.Width = 100; btnTras.Height = 28;
        btnTras.Click += (_, _) => AgregarTraslado();
        pTras.Controls.Add(btnTras);
        flow.Controls.Add(pTras);

        gridTraslados.Height = 100; gridTraslados.Width = 1160;
        flow.Controls.Add(gridTraslados);

        var lblPat = new Label { Text = "══  PATROCINIOS  ══", AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DarkGreen, Margin = new Padding(0, 8, 0, 4) };
        flow.Controls.Add(lblPat);

        var pPat = new Panel { Height = 80, Width = 950 };
        pPat.Controls.Add(CrearLabel("Patrocinador:", 0, 12));
        cboPatrocinador.Location = new Point(100, 9); cboPatrocinador.Width = 200; pPat.Controls.Add(cboPatrocinador);
        pPat.Controls.Add(CrearLabel("Edición:", 315, 12));
        cboEdicionPat.Location = new Point(380, 9); cboEdicionPat.Width = 160; pPat.Controls.Add(cboEdicionPat);
        pPat.Controls.Add(CrearLabel("Tipo:", 555, 12));
        cboTipoAporte.Location = new Point(595, 9); pPat.Controls.Add(cboTipoAporte);
        pPat.Controls.Add(CrearLabel("Monto Bs:", 700, 12));
        nudMonto.Location = new Point(775, 9); pPat.Controls.Add(nudMonto);
        var lblMin = CrearLabel("mín. 1", 880, 12); lblMin.ForeColor = Color.Gray; lblMin.Font = new Font("Segoe UI", 8f);
        pPat.Controls.Add(lblMin);
        pPat.Controls.Add(CrearLabel("Descripción:", 0, 48));
        txtDescAporte.Location = new Point(100, 45); txtDescAporte.Width = 500; pPat.Controls.Add(txtDescAporte);
        var btnPat = NuevoBoton("➕ Registrar Patrocinio"); btnPat.Location = new Point(620, 42);
        btnPat.Click += (_, _) => AgregarPatrocinio();
        pPat.Controls.Add(btnPat);
        flow.Controls.Add(pPat);

        gridPatrocinios.Height = 150; gridPatrocinios.Width = 1160;
        flow.Controls.Add(gridPatrocinios);

        tab.Controls.Add(flow);
        return tab;
    }

    private TabPage CrearTabEventos()
    {
        var tab = new TabPage("🎪 Eventos Paralelos");

        var panelSup = NuevoPanelSuperior(50);
        panelSup.Controls.Add(Etiquetado("Seleccionar evento:", cboEvento));

        gridEventos.Dock = DockStyle.Top;
        gridEventos.Height = 120;
        tab.Controls.Add(gridEventos);
        tab.Controls.Add(panelSup);

        var panelInf = NuevoPanelSuperior(120);
        panelInf.Controls.Add(Etiquetado("Expositor:", cboNuevoExpositor));
        var btnAgregarExp = NuevoBoton("➕ Asignar Expositor");
        btnAgregarExp.Click += (_, _) => AgregarExpositor();
        panelInf.Controls.Add(btnAgregarExp);
        var btnQuitarExp = NuevoBoton("➖ Quitar Expositor");
        btnQuitarExp.Click += (_, _) => QuitarExpositor();
        panelInf.Controls.Add(btnQuitarExp);
        tab.Controls.Add(panelInf);

        gridExpositores.Dock = DockStyle.Top;
        gridExpositores.Height = 130;
        tab.Controls.Add(gridExpositores);

        gridAsistentesEv.Dock = DockStyle.Fill;
        tab.Controls.Add(gridAsistentesEv);

        cboEvento.SelectedIndexChanged += (_, _) => CargarDetalleEvento();

        return tab;
    }

    private TabPage CrearTabCompetencia()
    {
        var tab = new TabPage("🎬 Jurados y Competencia");
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            Padding = new Padding(10) };

        var panelSup = new Panel { Height = 50, Width = 900 };
        panelSup.Controls.Add(new Label { Text = "Categoría:", AutoSize = true, Top = 12 });
        cboCategoria.Location = new Point(80, 10);
        cboCategoria.Width = 350;
        panelSup.Controls.Add(cboCategoria);
        flow.Controls.Add(panelSup);
        cboCategoria.SelectedIndexChanged += (_, _) => CargarDetalleCategoria();

        gridMiembrosCat.Height = 90; gridMiembrosCat.Width = 900;
        flow.Controls.Add(gridMiembrosCat);
        flow.SetFlowBreak(gridMiembrosCat, true);

        var lblEval = new Label { Text = "══  REGISTRAR EVALUACIÓN  ══", AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DarkBlue,
            Margin = new Padding(0, 6, 0, 2) };
        flow.Controls.Add(lblEval);

        var panelEval = new Panel { Height = 38, Width = 900 };
        var lblM = new Label { Text = "Miembro:", AutoSize = true, Top = 10 }; lblM.Location = new Point(0, 10);
        cboMiembroEval.Location = new Point(70, 7); cboMiembroEval.Width = 200;
        var lblP = new Label { Text = "Película:", AutoSize = true, Top = 10 }; lblP.Location = new Point(290, 10);
        cboPeliculaEval.Location = new Point(350, 7); cboPeliculaEval.Width = 200;
        var lblPt = new Label { Text = "Puntaje (1-10):", AutoSize = true, Top = 10 }; lblPt.Location = new Point(570, 10);
        nudPuntuacion.Location = new Point(670, 5);
        panelEval.Controls.AddRange(new Control[] { lblM, cboMiembroEval, lblP, cboPeliculaEval, lblPt, nudPuntuacion });
        flow.Controls.Add(panelEval);

        txtComentario.Height = 50; txtComentario.Width = 400;
        var panelCom = new Panel { Height = 60, Width = 900 };
        var lblC = new Label { Text = "Comentario:", AutoSize = true, Top = 15 };
        txtComentario.Location = new Point(85, 5);
        var btnEval = NuevoBoton("⭐ Registrar Evaluación");
        btnEval.Location = new Point(500, 8);
        btnEval.Click += (_, _) => RegistrarEvaluacion();
        panelCom.Controls.AddRange(new Control[] { lblC, txtComentario, btnEval });
        flow.Controls.Add(panelCom);

        gridEval.Height = 120; gridEval.Width = 900;
        flow.Controls.Add(gridEval);

        var lblPrem = new Label { Text = "══  REGISTRAR PREMIO (Ganador por Categoría)  ══", AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.DarkGreen,
            Margin = new Padding(0, 6, 0, 2) };
        flow.Controls.Add(lblPrem);

        var panelPrem = new Panel { Height = 38, Width = 900 };
        var lblCP = new Label { Text = "Categoría:", AutoSize = true, Top = 10 }; lblCP.Location = new Point(0, 10);
        cboCategoriaPrem.Location = new Point(70, 7); cboCategoriaPrem.Width = 200;
        var lblGP = new Label { Text = "Película ganadora:", AutoSize = true, Top = 10 }; lblGP.Location = new Point(290, 10);
        cboPeliculaPrem.Location = new Point(410, 7); cboPeliculaPrem.Width = 200;
        var btnPrem = NuevoBoton("🏅 Registrar Premio");
        btnPrem.Location = new Point(630, 3);
        btnPrem.Click += (_, _) => RegistrarPremio();
        panelPrem.Controls.AddRange(new Control[] { lblCP, cboCategoriaPrem, lblGP, cboPeliculaPrem, btnPrem });
        flow.Controls.Add(panelPrem);

        gridPremios.Height = 180; gridPremios.Width = 900;
        flow.Controls.Add(gridPremios);

        tab.Controls.Add(flow);
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

            DataTable sedes = ProcedimientosBD.ListarSedes();
            EnlazarCombo(cboSedeSala, sedes, "NombreSede", "IdSede");

            DataTable generos = ProcedimientosBD.ListarGeneros();
            chkGenerosPeli.DataSource = generos;
            chkGenerosPeli.DisplayMember = "NombreGenero";
            chkGenerosPeli.ValueMember = "IdGenero";

            nudAnioPeli.Value = DateTime.Today.Year;
            dtpFechaHora.Value = DateTime.Today.AddDays(1).AddHours(19);

            CargarProyeccionesCombo();
            RefrescarGrillasProyecciones();
            CargarAbonosGrilla();
            CargarAsistentesGrilla();
            CargarEventosIniciales();
            CargarLogisticaInicial();
            CargarCompetenciaInicial();
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

    private void CargarAsistentesGrilla() =>
        Intentar(() => gridAsistentes.DataSource = ProcedimientosBD.ListarAsistentes());

    /* ── Logística y Patrocinios ── */

    private void CargarLogisticaInicial()
    {
        Intentar(() =>
        {
            EnlazarCombo(cboInvitadoAloj, ProcedimientosBD.ListarPersonal(), "Nombre", "IdPersonal");
            EnlazarCombo(cboInvitadoTras, ProcedimientosBD.ListarPersonal(), "Nombre", "IdPersonal");
            EnlazarCombo(cboHotel, ProcedimientosBD.ListarHoteles(), "NombreHotel", "IdHotel");
            EnlazarCombo(cboPatrocinador, ProcedimientosBD.ListarPatrocinadores(), "NombreEmpresa", "IdPatrocinador");
            var eds = ProcedimientosBD.ListarEdiciones();
            AgregarColumnaDescriptiva(eds, r => $"{r["Anio"]} - {r["NombreEdicion"]}");
            EnlazarCombo(cboEdicionPat, eds, "Descripcion_UI", "IdEdicion");
            gridAlojamientos.DataSource = ProcedimientosBD.ListarAlojamientos();
            gridTraslados.DataSource = ProcedimientosBD.ListarTraslados();
            gridPatrocinios.DataSource = ProcedimientosBD.ListarPatrocinioEdicion();
        });
    }

    private void AgregarAlojamiento()
    {
        if (cboInvitadoAloj.SelectedItem is not DataRowView di ||
            cboHotel.SelectedItem is not DataRowView dh) { Aviso("Seleccione invitado y hotel."); return; }
        if (string.IsNullOrEmpty(cboHabitacion.Text)) { Aviso("Seleccione el número de habitación."); return; }
        Intentar(() =>
        {
            var r = ProcedimientosBD.RegistrarAlojamiento(
                Convert.ToInt32(di.Row["IdPersonal"]), Convert.ToInt32(dh.Row["IdHotel"]),
                cboHabitacion.Text, dtpCheckIn.Value, dtpCheckOut.Value);
            Exito(r);
            gridAlojamientos.DataSource = ProcedimientosBD.ListarAlojamientos();
        });
    }

    private void AgregarTraslado()
    {
        if (cboInvitadoTras.SelectedItem is not DataRowView di) { Aviso("Seleccione un invitado."); return; }
        if (string.IsNullOrEmpty(cboTipoTraslado.Text)) { Aviso("Seleccione el tipo de traslado."); return; }
        if (string.IsNullOrEmpty(cboOrigen.Text)) { Aviso("Seleccione o escriba el origen."); return; }
        if (string.IsNullOrEmpty(cboDestino.Text)) { Aviso("Seleccione o escriba el destino."); return; }
        Intentar(() =>
        {
            var r = ProcedimientosBD.RegistrarTraslado(
                Convert.ToInt32(di.Row["IdPersonal"]), cboTipoTraslado.Text,
                cboOrigen.Text, cboDestino.Text, dtpFechaTras.Value, cboVuelo.Text);
            Exito(r);
            gridTraslados.DataSource = ProcedimientosBD.ListarTraslados();
        });
    }

    private void AgregarPatrocinio()
    {
        if (cboPatrocinador.SelectedItem is not DataRowView dp ||
            cboEdicionPat.SelectedItem is not DataRowView de) { Aviso("Seleccione patrocinador y edición."); return; }
        if (string.IsNullOrEmpty(cboTipoAporte.Text)) { Aviso("Seleccione el tipo de aporte."); return; }
        if (cboTipoAporte.Text == "Economico" && nudMonto.Value <= 0) { Aviso("Ingrese un monto válido para el aporte económico."); return; }
        Intentar(() =>
        {
            decimal? monto = cboTipoAporte.Text == "Economico" ? nudMonto.Value : null;
            var r = ProcedimientosBD.RegistrarPatrocinio(
                Convert.ToInt32(dp.Row["IdPatrocinador"]), Convert.ToInt32(de.Row["IdEdicion"]),
                cboTipoAporte.Text, monto, txtDescAporte.Text);
            Exito(r);
            gridPatrocinios.DataSource = ProcedimientosBD.ListarPatrocinioEdicion();
        });
    }

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
            if (gridPremiacion.Columns["PromedioJurado"] != null)
                gridPremiacion.Columns["PromedioJurado"].DefaultCellStyle.Format = "N2";
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
            string respuesta = ProcedimientosBD.VenderAbono(idAsistente, idTipoAbono, pagoExitoso);
            Exito(respuesta);
            CargarAbonosGrilla();
        });
    }

    private void CrearPelicula()
    {
        if (string.IsNullOrWhiteSpace(txtTituloPeli.Text)) { Aviso("Ingrese el título de la película."); return; }
        if (string.IsNullOrWhiteSpace(txtPaisPeli.Text)) { Aviso("Ingrese el país de origen."); return; }
        if (cboClasifPeli.SelectedItem == null) { Aviso("Seleccione una clasificación."); return; }
        if (cboFormatoPeli.SelectedItem == null) { Aviso("Seleccione un formato."); return; }
        if (cboEstadoPeli.SelectedItem == null) { Aviso("Seleccione un estado."); return; }
        if (chkGenerosPeli.CheckedItems.Count == 0) { Aviso("Seleccione al menos un género."); return; }
        Intentar(() =>
        {
            var (respuesta, idPelicula) = ProcedimientosBD.CrearPelicula(
                txtTituloPeli.Text.Trim(), (int)nudAnioPeli.Value, (int)nudDuracionPeli.Value,
                txtPaisPeli.Text.Trim(), txtSinopsisPeli.Text.Trim(),
                cboClasifPeli.Text, cboFormatoPeli.Text, cboEstadoPeli.Text);
            foreach (var item in chkGenerosPeli.CheckedItems)
            {
                if (item is DataRowView row)
                    ProcedimientosBD.AgregarGeneroPelicula(idPelicula,
                        Convert.ToInt32(row.Row["IdGenero"]));
            }
            Exito(respuesta);
            DataTable peliculas = ProcedimientosBD.ListarPeliculas();
            EnlazarCombo(cboPelicula, peliculas.Copy(), "Titulo", "IdPelicula");
            EnlazarCombo(cboPeliculaAg, peliculas.Copy(), "Titulo", "IdPelicula");
            LimpiarFormularioPelicula();
        });
    }

    private void LimpiarFormularioPelicula()
    {
        txtTituloPeli.Clear(); txtPaisPeli.Clear(); txtSinopsisPeli.Clear();
        nudAnioPeli.Value = DateTime.Today.Year;
        nudDuracionPeli.Value = 1;
        cboClasifPeli.SelectedIndex = 0;
        cboFormatoPeli.SelectedIndex = 0;
        cboEstadoPeli.SelectedIndex = 0;
        for (int i = 0; i < chkGenerosPeli.Items.Count; i++)
            chkGenerosPeli.SetItemChecked(i, false);
    }

    private void CrearSala()
    {
        if (string.IsNullOrWhiteSpace(txtNombreSala.Text)) { Aviso("Ingrese el nombre de la sala."); return; }
        if (cboSedeSala.SelectedValue is not int idSede) { Aviso("Seleccione una sede."); return; }
        Intentar(() =>
        {
            string respuesta = ProcedimientosBD.CrearSala(
                txtNombreSala.Text.Trim(), (int)nudCapacidadSala.Value, idSede);
            Exito(respuesta);
            DataTable salas = ProcedimientosBD.ListarSalas();
            AgregarColumnaDescriptiva(salas, r => $"{r["NombreSala"]} — {r["NombreSede"]} (Cap. {r["Capacidad"]})");
            EnlazarCombo(cboSala, salas, "Descripcion_UI", "IdSala");
            txtNombreSala.Clear();
            nudCapacidadSala.Value = 1;
        });
    }

    private void RegistrarAsistente()
    {
        if (string.IsNullOrWhiteSpace(txtNombreAsist.Text)) { Aviso("Ingrese el nombre del asistente."); return; }
        if (string.IsNullOrWhiteSpace(txtEmailAsist.Text)) { Aviso("Ingrese el email del asistente."); return; }
        if (cboTipoAsist.SelectedItem == null) { Aviso("Seleccione el tipo de asistente."); return; }
        Intentar(() =>
        {
            string respuesta = ProcedimientosBD.CrearAsistente(
                txtNombreAsist.Text.Trim(), txtEmailAsist.Text.Trim(),
                txtTelAsist.Text.Trim(), cboTipoAsist.Text);
            Exito(respuesta);
            DataTable asistentes = ProcedimientosBD.ListarAsistentes();
            EnlazarCombo(cboAsistente, asistentes.Copy(), "Nombre", "IdAsistente");
            EnlazarCombo(cboAsistenteAb, asistentes.Copy(), "Nombre", "IdAsistente");
            txtNombreAsist.Clear(); txtEmailAsist.Clear(); txtTelAsist.Clear();
            cboTipoAsist.SelectedIndex = 0;
            gridAsistentes.DataSource = ProcedimientosBD.ListarAsistentes();
        });
    }

    /* ── Eventos Paralelos ────────────────────────────────── */

    private void CargarEventosIniciales()
    {
        Intentar(() =>
        {
            DataTable eventos = ProcedimientosBD.ListarEventosParalelos();
            AgregarColumnaDescriptiva(eventos, r =>
                $"{r["NombreEvento"]} — {((DateTime)r["FechaHora"]):dd/MM/yyyy HH:mm} (Aforo: {r["Aforo"]})");
            EnlazarCombo(cboEvento, eventos, "Descripcion_UI", "IdEvento");

            DataTable personal = ProcedimientosBD.ListarPersonal();
            EnlazarCombo(cboNuevoExpositor, personal, "Nombre", "IdPersonal");

            gridEventos.DataSource = ProcedimientosBD.ListarEventosParalelos();
            CargarDetalleEvento();
        });
    }

    private void CargarDetalleEvento()
    {
        if (cboEvento.SelectedItem is not DataRowView drv) { gridExpositores.DataSource = null; gridAsistentesEv.DataSource = null; return; }
        Intentar(() =>
        {
            int idEvento = Convert.ToInt32(drv.Row["IdEvento"]);
            gridExpositores.DataSource = ProcedimientosBD.ListarExpositoresPorEvento(idEvento);
            gridAsistentesEv.DataSource = ProcedimientosBD.ListarAsistentesPorEvento(idEvento);
        });
    }

    private void AgregarExpositor()
    {
        if (cboEvento.SelectedItem is not DataRowView drvEvento ||
            cboNuevoExpositor.SelectedItem is not DataRowView drvExp)
        {
            Aviso("Seleccione un evento y un expositor.");
            return;
        }
        int idEvento = Convert.ToInt32(drvEvento.Row["IdEvento"]);
        int idPersonal = Convert.ToInt32(drvExp.Row["IdPersonal"]);
        Intentar(() =>
        {
            string respuesta = ProcedimientosBD.AgregarExpositorEvento(idEvento, idPersonal);
            Exito(respuesta);
            CargarDetalleEvento();
        });
    }

    private void QuitarExpositor()
    {
        if (cboEvento.SelectedItem is not DataRowView drvEvento) return;
        if (gridExpositores.CurrentRow?.DataBoundItem is DataRowView drv)
        {
            int idEvento = Convert.ToInt32(drvEvento.Row["IdEvento"]);
            int idPersonal = Convert.ToInt32(drv["IdPersonal"]);
            Intentar(() =>
            {
                string respuesta = ProcedimientosBD.EliminarExpositorEvento(idEvento, idPersonal);
                Exito(respuesta);
                CargarDetalleEvento();
            });
        }
        else
        {
            Aviso("Seleccione un expositor de la lista para quitar.");
        }
    }

    /* ── Competencia y Jurados ─────────────────────────────── */

    private void CargarCompetenciaInicial()
    {
        Intentar(() =>
        {
            DataTable cats = ProcedimientosBD.ListarCategorias();
            EnlazarCombo(cboCategoria, cats.Copy(), "NombreCategoria", "IdCategoria");
            EnlazarCombo(cboCategoriaPrem, cats.Copy(), "NombreCategoria", "IdCategoria");
            gridPremios.DataSource = ProcedimientosBD.ListarPremios();
            CargarDetalleCategoria();
        });
    }

    private void CargarDetalleCategoria()
    {
        if (cboCategoria.SelectedItem is not DataRowView drv) return;
        int idCat = Convert.ToInt32(drv.Row["IdCategoria"]);
        Intentar(() =>
        {
            gridMiembrosCat.DataSource = ProcedimientosBD.ListarMiembrosPorCategoria(idCat);
            gridEval.DataSource = ProcedimientosBD.ListarEvaluacionesPorCategoria(idCat);

            DataTable miembros = ProcedimientosBD.ListarMiembrosPorCategoria(idCat);
            EnlazarCombo(cboMiembroEval, miembros, "Nombre", "IdMiembro");

            DataTable pelis = ProcedimientosBD.ListarPeliculasEnCompetencia(idCat);
            EnlazarCombo(cboPeliculaEval, pelis.Copy(), "Titulo", "IdPelicula");
            EnlazarCombo(cboPeliculaPrem, pelis.Copy(), "Titulo", "IdPelicula");
        });
    }

    private void RegistrarEvaluacion()
    {
        if (cboMiembroEval.SelectedItem is not DataRowView drvM ||
            cboPeliculaEval.SelectedItem is not DataRowView drvP ||
            cboCategoria.SelectedItem is not DataRowView drvC)
        { Aviso("Seleccione miembro, película y categoría."); return; }
        Intentar(() =>
        {
            string r = ProcedimientosBD.RegistrarEvaluacion(
                Convert.ToInt32(drvM.Row["IdMiembro"]),
                Convert.ToInt32(drvP.Row["IdPelicula"]),
                Convert.ToInt32(drvC.Row["IdCategoria"]),
                (int)nudPuntuacion.Value,
                txtComentario.Text);
            Exito(r);
            CargarDetalleCategoria();
            txtComentario.Clear();
        });
    }

    private void RegistrarPremio()
    {
        if (cboCategoriaPrem.SelectedItem is not DataRowView drvC ||
            cboPeliculaPrem.SelectedItem is not DataRowView drvP)
        { Aviso("Seleccione categoría y película ganadora."); return; }
        Intentar(() =>
        {
            string r = ProcedimientosBD.RegistrarPremio(
                Convert.ToInt32(drvC.Row["IdCategoria"]),
                Convert.ToInt32(drvP.Row["IdPelicula"]),
                DateTime.Now.Year);
            Exito(r);
            gridPremios.DataSource = ProcedimientosBD.ListarPremios();
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

    private static Label CrearLabel(string texto, int x, int y) => new()
    {
        Text = texto,
        AutoSize = true,
        Location = new Point(x, y),
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(55, 60, 70)
    };

    private static ComboBox NuevoCombo()
    {
        var c = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 280,
            FlatStyle = FlatStyle.Flat
        };
        return c;
    }

    private static DataGridView NuevaGrilla()
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(220, 220, 225),
            RowTemplate = new DataGridViewRow { MinimumHeight = 26 }
        };
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(55, 60, 70),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0)
        };
        g.ColumnHeadersHeight = 32;
        g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(245, 245, 250)
        };
        g.DefaultCellStyle = new DataGridViewCellStyle
        {
            ForeColor = Color.FromArgb(40, 40, 45),
            Padding = new Padding(4, 0, 0, 0)
        };
        g.RowTemplate.Height = 26;
        return g;
    }

    private static Label NuevaEtiquetaEstado() => new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(30, 130, 70),
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        Margin = new Padding(10, 12, 0, 0)
    };

    private static FlowLayoutPanel NuevoPanelSuperior(int alto) => new()
    {
        Dock = DockStyle.Top,
        Height = alto,
        Padding = new Padding(10),
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = true,
        BackColor = Color.FromArgb(248, 248, 252)
    };

    private static Button NuevoBoton(string texto)
    {
        var b = new Button
        {
            Text = texto,
            AutoSize = true,
            Padding = new Padding(10, 5, 10, 5),
            Margin = new Padding(10, 18, 0, 0),
            BackColor = Color.FromArgb(200, 30, 45),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(170, 20, 35);
        b.FlatAppearance.MouseDownBackColor = Color.FromArgb(140, 15, 25);
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private static Panel Etiquetado(string texto, Control control)
    {
        var contenedor = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Margin = new Padding(0, 0, 12, 0)
        };
        contenedor.Controls.Add(new Label
        {
            Text = texto,
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 80, 90),
            Font = new Font("Segoe UI", 8.5f)
        });
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
