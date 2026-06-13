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

    private readonly TextBox txtNombreSede = new() { Width = 140 };
    private readonly TextBox txtDirSede = new() { Width = 180 };
    private readonly TextBox txtCiudadSede = new() { Width = 120 };
    private readonly TextBox txtWebSede = new() { Width = 140 };

    private readonly TextBox txtNombreAsist = new() { Width = 160 };
    private readonly TextBox txtEmailAsist = new() { Width = 160 };
    private readonly TextBox txtTelAsist = new() { Width = 110 };
    private readonly ComboBox cboProfesionAsist = new() { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox txtProfesionOtros = new() { Width = 130, PlaceholderText = "especifique..." };
    private readonly Panel panelProfesion = new() { Width = 280, Height = 28 };
    private readonly ComboBox cboTipoAsist = NuevoCombo();
    private readonly Label lblCategorias = CrearLabel("Categorías:", 0, 6);
    private readonly CheckedListBox clbCategorias = new() { Width = 230, Height = 90, CheckOnClick = true };
    private readonly DataGridView gridAsistentes = NuevaGrilla();

    /* ── Abonos (T1) ── */
    private readonly ComboBox cboAsistenteAb = NuevoCombo();
    private readonly ComboBox cboTipoAbono   = NuevoCombo();
    private readonly DataGridView gridAbonos = NuevaGrilla();

    /* ── Eventos Paralelos ── */
    private readonly ComboBox cboEvento       = NuevoCombo();
    private readonly ComboBox cboAsistenteEvento = NuevoCombo();
    private readonly DataGridView gridEventos = NuevaGrilla();
    private readonly DataGridView gridExpositores = NuevaGrilla();
    private readonly DataGridView gridAsistentesEv = NuevaGrilla();
    private readonly ComboBox cboNuevoExpositor = NuevoCombo();
    private readonly TextBox txtNombreEvento = new() { Width = 160 };
    private readonly ComboBox cboTipoEvento = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly DateTimePicker dtpFechaEvento = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", ShowUpDown = true, Width = 150 };
    private readonly NumericUpDown nudAforoEvento = new() { Width = 80, Maximum = 99999, Minimum = 1 };
    private readonly NumericUpDown nudCostoEvento = new() { Width = 100, DecimalPlaces = 2, Maximum = 999999, Minimum = 0 };
    private readonly DataGridView gridPeliculas = NuevaGrilla();
    private readonly DataGridView gridSedes = NuevaGrilla();
    private readonly DataGridView gridSalasList = NuevaGrilla();

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
    private readonly ComboBox cboAnioRanking    = NuevoCombo();
    private readonly ComboBox cboAnioPremiacion = NuevoCombo();
    private readonly ComboBox cboAnioFinanciero = NuevoCombo();

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
        tabs.TabPages.Add(CrearTabReporte("🏆 Ranking", gridRanking, lblRanking, cboAnioRanking, CargarRanking));
        tabs.TabPages.Add(CrearTabReporte("🎖 Premiación", gridPremiacion, lblPremiacion, cboAnioPremiacion, CargarPremiacion));
        tabs.TabPages.Add(CrearTabReporte("💰 Financiero", gridFinanciero, lblFinanciero, cboAnioFinanciero, CargarFinanciero));
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
        cboProfesionAsist.Items.AddRange(new[] { "Actor", "Crítico", "Director", "Otros" });
        cboProfesionAsist.SelectedIndex = -1;
        cboProfesionAsist.Location = new Point(0, 0);
        txtProfesionOtros.Location = new Point(145, 0);
        txtProfesionOtros.Visible = false;
        panelProfesion.Controls.Add(cboProfesionAsist);
        panelProfesion.Controls.Add(txtProfesionOtros);
        panelProfesion.Visible = false;
        cboProfesionAsist.SelectedIndexChanged += (_, _) =>
        {
            txtProfesionOtros.Visible = cboProfesionAsist.Text == "Otros";
            if (cboProfesionAsist.Text == "Otros") txtProfesionOtros.Focus();
        };
        panelAsist.Controls.Add(Etiquetado("Profesión:", panelProfesion));
        lblCategorias.Visible = false;
        panelAsist.Controls.Add(lblCategorias);
        clbCategorias.Visible = false;
        panelAsist.Controls.Add(clbCategorias);
        cboTipoAsist.SelectedIndexChanged += (_, _) =>
        {
            bool esJurado = cboTipoAsist.Text == "Jurado";
            panelProfesion.Visible = esJurado;
            lblCategorias.Visible = esJurado;
            clbCategorias.Visible = esJurado;
            if (esJurado)
                Intentar(() => EnlazarCheckedListBox(clbCategorias, ProcedimientosBD.ListarCategorias(), "NombreCategoria", "IdCategoria"));
        };
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

        cboClasifPeli.Items.AddRange(new[] { "G", "PG", "PG-13", "R", "NC-17" });
        cboClasifPeli.SelectedIndex = 0;
        cboFormatoPeli.Items.AddRange(new[] { "2D", "3D", "IMAX", "4DX" });
        cboFormatoPeli.SelectedIndex = 0;
        cboEstadoPeli.Items.AddRange(new[] { "Postulada", "Seleccionada", "Rechazada", "Premiada" });
        cboEstadoPeli.SelectedIndex = 0;

        var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(5) };
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

        var gbPeli = new GroupBox { Text = "Películas", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpPeli = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpPeli.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        tlpPeli.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowPeli = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowPeli.Controls.Add(Etiquetado("Título:", txtTituloPeli));
        flowPeli.Controls.Add(Etiquetado("Año:", nudAnioPeli));
        flowPeli.Controls.Add(Etiquetado("Duración:", nudDuracionPeli));
        flowPeli.Controls.Add(Etiquetado("País:", txtPaisPeli));
        flowPeli.Controls.Add(Etiquetado("Clasificación:", cboClasifPeli));
        flowPeli.Controls.Add(Etiquetado("Formato:", cboFormatoPeli));
        flowPeli.Controls.Add(Etiquetado("Estado:", cboEstadoPeli));
        flowPeli.Controls.Add(Etiquetado("Sinopsis:", txtSinopsisPeli));
        flowPeli.Controls.Add(Etiquetado("Géneros:", chkGenerosPeli));
        var btnPeli = NuevoBoton("➕ Película");
        btnPeli.Margin = new Padding(10, 18, 0, 0);
        btnPeli.Click += (_, _) => CrearPelicula();
        flowPeli.Controls.Add(btnPeli);
        tlpPeli.Controls.Add(flowPeli, 0, 0);
        gridPeliculas.Dock = DockStyle.Fill;
        tlpPeli.Controls.Add(gridPeliculas, 0, 1);
        gbPeli.Controls.Add(tlpPeli);
        tlp.Controls.Add(gbPeli, 0, 0);

        var gbSede = new GroupBox { Text = "Sedes", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpSede = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpSede.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        tlpSede.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowSede = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowSede.Controls.Add(Etiquetado("Nombre:", txtNombreSede));
        flowSede.Controls.Add(Etiquetado("Dirección:", txtDirSede));
        flowSede.Controls.Add(Etiquetado("Ciudad:", txtCiudadSede));
        flowSede.Controls.Add(Etiquetado("Sitio web:", txtWebSede));
        var btnSede = NuevoBoton("➕ Sede");
        btnSede.Margin = new Padding(10, 18, 0, 0);
        btnSede.Click += (_, _) => RegistrarSede();
        flowSede.Controls.Add(btnSede);
        tlpSede.Controls.Add(flowSede, 0, 0);
        gridSedes.Dock = DockStyle.Fill;
        tlpSede.Controls.Add(gridSedes, 0, 1);
        gbSede.Controls.Add(tlpSede);
        tlp.Controls.Add(gbSede, 0, 1);

        var gbSala = new GroupBox { Text = "Salas", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpSala = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpSala.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        tlpSala.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowSala = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowSala.Controls.Add(Etiquetado("Nombre:", txtNombreSala));
        flowSala.Controls.Add(Etiquetado("Capacidad:", nudCapacidadSala));
        flowSala.Controls.Add(Etiquetado("Sede:", cboSedeSala));
        var btnSala = NuevoBoton("➕ Sala");
        btnSala.Margin = new Padding(10, 18, 0, 0);
        btnSala.Click += (_, _) => CrearSala();
        flowSala.Controls.Add(btnSala);
        tlpSala.Controls.Add(flowSala, 0, 0);
        gridSalasList.Dock = DockStyle.Fill;
        tlpSala.Controls.Add(gridSalasList, 0, 1);
        gbSala.Controls.Add(tlpSala);
        tlp.Controls.Add(gbSala, 0, 2);

        tab.Controls.Add(tlp);
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

        cboHabitacion.Items.AddRange(new[] { "101","102","103","104","201","202","203","204","301","302","303","304","401","402","403","404","501","502" });
        cboTipoTraslado.Items.AddRange(new[] { "Vuelo", "Transfer", "Taxi" });
        cboOrigen.Items.AddRange(new[] { "Santa Cruz (VVI)","La Paz (LPB)","Cochabamba (CBB)","Buenos Aires (EZE)","Lima (LIM)","Santiago (SCL)","Madrid (MAD)","Miami (MIA)" });
        cboDestino.Items.AddRange(new[] { "Santa Cruz (VVI)","La Paz (LPB)","Cochabamba (CBB)","Buenos Aires (EZE)","Lima (LIM)","Santiago (SCL)","Madrid (MAD)","Miami (MIA)" });
        cboVuelo.Items.AddRange(new[] { "OB101","LA832","LA2081","AM543","CM216","AA900","IB6780","AV125" });
        cboTipoAporte.Items.AddRange(new[] { "Economico", "Especie" });
        cboTipoAporte.SelectedIndexChanged += (_, _) => nudMonto.Enabled = cboTipoAporte.Text == "Economico";

        var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(5) };
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

        var gbAloj = new GroupBox { Text = "Alojamientos", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpAloj = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpAloj.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        tlpAloj.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowAloj = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowAloj.Controls.Add(Etiquetado("Invitado:", cboInvitadoAloj));
        flowAloj.Controls.Add(Etiquetado("Hotel:", cboHotel));
        flowAloj.Controls.Add(Etiquetado("Habitación:", cboHabitacion));
        flowAloj.Controls.Add(Etiquetado("Check-in:", dtpCheckIn));
        flowAloj.Controls.Add(Etiquetado("Check-out:", dtpCheckOut));
        var btnAloj = NuevoBoton("➕ Alojar");
        btnAloj.Margin = new Padding(10, 18, 0, 0);
        btnAloj.Click += (_, _) => AgregarAlojamiento();
        flowAloj.Controls.Add(btnAloj);
        tlpAloj.Controls.Add(flowAloj, 0, 0);
        gridAlojamientos.Dock = DockStyle.Fill;
        tlpAloj.Controls.Add(gridAlojamientos, 0, 1);
        gbAloj.Controls.Add(tlpAloj);
        tlp.Controls.Add(gbAloj, 0, 0);

        var gbTras = new GroupBox { Text = "Traslados", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpTras = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpTras.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        tlpTras.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowTras = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowTras.Controls.Add(Etiquetado("Invitado:", cboInvitadoTras));
        flowTras.Controls.Add(Etiquetado("Tipo:", cboTipoTraslado));
        flowTras.Controls.Add(Etiquetado("Origen:", cboOrigen));
        flowTras.Controls.Add(Etiquetado("Destino:", cboDestino));
        flowTras.Controls.Add(Etiquetado("Fecha:", dtpFechaTras));
        flowTras.Controls.Add(Etiquetado("Vuelo:", cboVuelo));
        var btnTras = NuevoBoton("➕ Trasladar");
        btnTras.Margin = new Padding(10, 18, 0, 0);
        btnTras.Click += (_, _) => AgregarTraslado();
        flowTras.Controls.Add(btnTras);
        tlpTras.Controls.Add(flowTras, 0, 0);
        gridTraslados.Dock = DockStyle.Fill;
        tlpTras.Controls.Add(gridTraslados, 0, 1);
        gbTras.Controls.Add(tlpTras);
        tlp.Controls.Add(gbTras, 0, 1);

        var gbPat = new GroupBox { Text = "Patrocinios", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpPat = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpPat.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        tlpPat.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowPat = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowPat.Controls.Add(Etiquetado("Patrocinador:", cboPatrocinador));
        flowPat.Controls.Add(Etiquetado("Edición:", cboEdicionPat));
        flowPat.Controls.Add(Etiquetado("Tipo:", cboTipoAporte));
        flowPat.Controls.Add(Etiquetado("Monto Bs:", nudMonto));
        flowPat.Controls.Add(Etiquetado("Descripción:", txtDescAporte));
        var btnPat = NuevoBoton("➕ Registrar Patrocinio");
        btnPat.Margin = new Padding(10, 18, 0, 0);
        btnPat.Click += (_, _) => AgregarPatrocinio();
        flowPat.Controls.Add(btnPat);
        tlpPat.Controls.Add(flowPat, 0, 0);
        gridPatrocinios.Dock = DockStyle.Fill;
        tlpPat.Controls.Add(gridPatrocinios, 0, 1);
        gbPat.Controls.Add(tlpPat);
        tlp.Controls.Add(gbPat, 0, 2);

        tab.Controls.Add(tlp);
        return tab;
    }

    private TabPage CrearTabEventos()
    {
        var tab = new TabPage("🎪 Eventos Paralelos");

        cboTipoEvento.Items.AddRange(new[] { "Masterclass", "Taller", "Coctel" });
        cboTipoEvento.SelectedIndex = 0;

        var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(5) };
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

        /* ── 1. Registrar Asistentes ── */
        var gbReg = new GroupBox { Text = "Registrar Asistentes", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpReg = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpReg.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        tlpReg.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowReg = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowReg.Controls.Add(Etiquetado("Evento:", cboEvento));
        flowReg.Controls.Add(Etiquetado("Asistente:", cboAsistenteEvento));
        var btnReg = NuevoBoton("➕ Registrar");
        btnReg.Margin = new Padding(10, 18, 0, 0);
        btnReg.Click += (_, _) => RegistrarAsistenteEvento();
        flowReg.Controls.Add(btnReg);
        tlpReg.Controls.Add(flowReg, 0, 0);
        gridAsistentesEv.Dock = DockStyle.Fill;
        tlpReg.Controls.Add(gridAsistentesEv, 0, 1);
        gbReg.Controls.Add(tlpReg);
        tlp.Controls.Add(gbReg, 0, 0);

        /* ── 2. Asignar Expositores ── */
        var gbExp = new GroupBox { Text = "Asignar Expositores", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpExp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpExp.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        tlpExp.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowExp = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowExp.Controls.Add(Etiquetado("Expositor:", cboNuevoExpositor));
        var btnAgregarExp = NuevoBoton("➕ Asignar Expositor");
        btnAgregarExp.Margin = new Padding(10, 18, 0, 0);
        btnAgregarExp.Click += (_, _) => AgregarExpositor();
        flowExp.Controls.Add(btnAgregarExp);
        var btnQuitarExp = NuevoBoton("➖ Quitar Expositor");
        btnQuitarExp.Margin = new Padding(5, 18, 0, 0);
        btnQuitarExp.Click += (_, _) => QuitarExpositor();
        flowExp.Controls.Add(btnQuitarExp);
        tlpExp.Controls.Add(flowExp, 0, 0);
        gridExpositores.Dock = DockStyle.Fill;
        tlpExp.Controls.Add(gridExpositores, 0, 1);
        gbExp.Controls.Add(tlpExp);
        tlp.Controls.Add(gbExp, 0, 1);

        /* ── 3. Crear Evento ── */
        var gbCrear = new GroupBox { Text = "Crear Evento Paralelo", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpCrear = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpCrear.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        tlpCrear.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowCrear = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowCrear.Controls.Add(Etiquetado("Nombre:", txtNombreEvento));
        flowCrear.Controls.Add(Etiquetado("Tipo:", cboTipoEvento));
        flowCrear.Controls.Add(Etiquetado("Fecha y hora:", dtpFechaEvento));
        flowCrear.Controls.Add(Etiquetado("Aforo:", nudAforoEvento));
        flowCrear.Controls.Add(Etiquetado("Costo Bs:", nudCostoEvento));
        var btnCrearEv = NuevoBoton("➕ Crear Evento");
        btnCrearEv.Margin = new Padding(10, 18, 0, 0);
        btnCrearEv.Click += (_, _) => CrearEventoParalelo();
        flowCrear.Controls.Add(btnCrearEv);
        tlpCrear.Controls.Add(flowCrear, 0, 0);
        gridEventos.Dock = DockStyle.Fill;
        tlpCrear.Controls.Add(gridEventos, 0, 1);
        gbCrear.Controls.Add(tlpCrear);
        tlp.Controls.Add(gbCrear, 0, 2);

        tab.Controls.Add(tlp);

        cboEvento.SelectedIndexChanged += (_, _) => CargarDetalleEvento();

        return tab;
    }

    private TabPage CrearTabCompetencia()
    {
        var tab = new TabPage("🎬 Jurados y Competencia");

        var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(5) };
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

        /* ── 1. Categorías y Jurados ── */
        var gbCat = new GroupBox { Text = "Categorías y Jurados", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpCat = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpCat.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        tlpCat.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowCat = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowCat.Controls.Add(Etiquetado("Categoría:", cboCategoria));
        tlpCat.Controls.Add(flowCat, 0, 0);
        gridMiembrosCat.Dock = DockStyle.Fill;
        tlpCat.Controls.Add(gridMiembrosCat, 0, 1);
        gbCat.Controls.Add(tlpCat);
        tlp.Controls.Add(gbCat, 0, 0);

        /* ── 2. Evaluaciones ── */
        var gbEval = new GroupBox { Text = "Registrar Evaluación", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpEval = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpEval.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        tlpEval.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowEval = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowEval.Controls.Add(Etiquetado("Miembro:", cboMiembroEval));
        flowEval.Controls.Add(Etiquetado("Película:", cboPeliculaEval));
        flowEval.Controls.Add(Etiquetado("Puntaje (1-10):", nudPuntuacion));
        flowEval.Controls.Add(Etiquetado("Comentario:", txtComentario));
        var btnEval = NuevoBoton("⭐ Registrar Evaluación");
        btnEval.Margin = new Padding(10, 18, 0, 0);
        btnEval.Click += (_, _) => RegistrarEvaluacion();
        flowEval.Controls.Add(btnEval);
        tlpEval.Controls.Add(flowEval, 0, 0);
        gridEval.Dock = DockStyle.Fill;
        tlpEval.Controls.Add(gridEval, 0, 1);
        gbEval.Controls.Add(tlpEval);
        tlp.Controls.Add(gbEval, 0, 1);

        /* ── 3. Premios ── */
        var gbPrem = new GroupBox { Text = "Registrar Premio (Ganador por Categoría)", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var tlpPrem = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
        tlpPrem.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        tlpPrem.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var flowPrem = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
        flowPrem.Controls.Add(Etiquetado("Categoría:", cboCategoriaPrem));
        flowPrem.Controls.Add(Etiquetado("Película ganadora:", cboPeliculaPrem));
        var btnPrem = NuevoBoton("🏅 Registrar Premio");
        btnPrem.Margin = new Padding(10, 18, 0, 0);
        btnPrem.Click += (_, _) => RegistrarPremio();
        flowPrem.Controls.Add(btnPrem);
        tlpPrem.Controls.Add(flowPrem, 0, 0);
        gridPremios.Dock = DockStyle.Fill;
        tlpPrem.Controls.Add(gridPremios, 0, 1);
        gbPrem.Controls.Add(tlpPrem);
        tlp.Controls.Add(gbPrem, 0, 2);

        tab.Controls.Add(tlp);

        cboCategoria.SelectedIndexChanged += (_, _) => CargarDetalleCategoria();

        return tab;
    }

    private static TabPage CrearTabReporte(string titulo, DataGridView grid, Label lblEstado,
        ComboBox cboAnio, Action cargar)
    {
        var tab = new TabPage(titulo);

        var panel = NuevoPanelSuperior(70);
        cboAnio.Width = 100;
        cboAnio.Items.Insert(0, "Todos");
        cboAnio.SelectedIndex = 0;
        panel.Controls.Add(Etiquetado("Año Edición:", cboAnio));
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
            RefrescarGrillasAdmin();
            CargarEdicionesCombo(cboAnioRanking);
            CargarEdicionesCombo(cboAnioPremiacion);
            CargarEdicionesCombo(cboAnioFinanciero);
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

    private void RefrescarGrillasAdmin()
    {
        Intentar(() =>
        {
            gridPeliculas.DataSource = ProcedimientosBD.ListarPeliculas();
            gridSedes.DataSource = ProcedimientosBD.ListarSedes();
            gridSalasList.DataSource = ProcedimientosBD.ListarSalas();
        });
    }

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

    private void CargarEdicionesCombo(ComboBox cbo)
    {
        DataTable ediciones = ProcedimientosBD.ListarEdiciones();
        cbo.Items.Clear();
        cbo.Items.Add("Todos");
        foreach (DataRow r in ediciones.Rows)
            cbo.Items.Add(r["Anio"]?.ToString() ?? "");
        cbo.SelectedIndex = 0;
    }

    private int? ObtenerAnioSeleccionado(ComboBox cbo)
    {
        if (cbo.SelectedIndex <= 0) return null;
        if (int.TryParse(cbo.Text, out int anio)) return anio;
        return null;
    }

    private void CargarRanking() =>
        Intentar(() =>
        {
            (DataTable datos, string respuesta) = ProcedimientosBD.ReporteRanking(ObtenerAnioSeleccionado(cboAnioRanking));
            gridRanking.DataSource = datos;
            lblRanking.Text = respuesta;
        });

    private void CargarPremiacion() =>
        Intentar(() =>
        {
            (DataTable datos, string respuesta) = ProcedimientosBD.ReportePremiacion(ObtenerAnioSeleccionado(cboAnioPremiacion));
            gridPremiacion.DataSource = datos;
            var colProm = gridPremiacion.Columns["PromedioJurado"];
            if (colProm != null)
                colProm.DefaultCellStyle.Format = "N2";
            lblPremiacion.Text = respuesta;
        });

    private void CargarFinanciero() =>
        Intentar(() =>
        {
            (DataTable datos, string respuesta) = ProcedimientosBD.ReporteFinanciero(ObtenerAnioSeleccionado(cboAnioFinanciero));
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
            if (!pagoExitoso)
                respuesta = "ROLLBACK ejecutado correctamente.\nLa venta del abono fue cancelada debido a un error en el proceso de pago";
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
            RefrescarGrillasAdmin();
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
            RefrescarGrillasAdmin();
        });
    }

    private void RegistrarSede()
    {
        if (string.IsNullOrWhiteSpace(txtNombreSede.Text)) { Aviso("Ingrese el nombre de la sede."); return; }
        Intentar(() =>
        {
            string respuesta = ProcedimientosBD.CrearSede(
                txtNombreSede.Text.Trim(), txtDirSede.Text.Trim(),
                txtCiudadSede.Text.Trim(), txtWebSede.Text.Trim());
            Exito(respuesta);
            DataTable sedes = ProcedimientosBD.ListarSedes();
            EnlazarCombo(cboSedeSala, sedes, "NombreSede", "IdSede");
            txtNombreSede.Clear(); txtDirSede.Clear(); txtCiudadSede.Clear(); txtWebSede.Clear();
            RefrescarGrillasAdmin();
        });
    }

    private void RegistrarAsistente()
    {
        if (string.IsNullOrWhiteSpace(txtNombreAsist.Text)) { Aviso("Ingrese el nombre del asistente."); return; }
        if (string.IsNullOrWhiteSpace(txtEmailAsist.Text)) { Aviso("Ingrese el email del asistente."); return; }
        if (cboTipoAsist.SelectedItem == null) { Aviso("Seleccione el tipo de asistente."); return; }
        if (cboTipoAsist.Text == "Jurado")
        {
            string prof = cboProfesionAsist.Text == "Otros" ? txtProfesionOtros.Text.Trim() : cboProfesionAsist.Text.Trim();
            if (string.IsNullOrWhiteSpace(prof)) { Aviso("Ingrese la profesión del jurado."); return; }
        }
        if (cboTipoAsist.Text == "Jurado" && clbCategorias.CheckedItems.Count == 0)
        { Aviso("Seleccione al menos una categoría para el jurado."); return; }
        Intentar(() =>
        {
            string profesion = cboProfesionAsist.Text == "Otros" ? txtProfesionOtros.Text.Trim() : cboProfesionAsist.Text.Trim();
            string respuesta = ProcedimientosBD.CrearAsistente(
                txtNombreAsist.Text.Trim(), txtEmailAsist.Text.Trim(),
                txtTelAsist.Text.Trim(), cboTipoAsist.Text,
                profesion);
            Exito(respuesta);
            if (cboTipoAsist.Text == "Jurado")
            {
                DataTable cats = ProcedimientosBD.ListarCategorias();
                string email = txtEmailAsist.Text.Trim();
                foreach (var item in clbCategorias.CheckedItems)
                {
                    var fila = cats.AsEnumerable().FirstOrDefault(r => r["NombreCategoria"]?.ToString() == item?.ToString());
                    if (fila != null)
                        ProcedimientosBD.AsignarCategoriaJurado(email, Convert.ToInt32(fila["IdCategoria"]));
                }
            }
            DataTable asistentes = ProcedimientosBD.ListarAsistentes();
            EnlazarCombo(cboAsistente, asistentes.Copy(), "Nombre", "IdAsistente");
            EnlazarCombo(cboAsistenteAb, asistentes.Copy(), "Nombre", "IdAsistente");
            txtNombreAsist.Clear(); txtEmailAsist.Clear(); txtTelAsist.Clear(); txtProfesionOtros.Clear();
            cboProfesionAsist.SelectedIndex = -1; txtProfesionOtros.Visible = false;
            cboTipoAsist.SelectedIndex = 0;
            clbCategorias.Items.Clear();
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

            DataTable asistentes = ProcedimientosBD.ListarAsistentes();
            EnlazarCombo(cboAsistenteEvento, asistentes, "Nombre", "IdAsistente");

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

    private void RegistrarAsistenteEvento()
    {
        if (cboEvento.SelectedItem is not DataRowView drvEvento ||
            cboAsistenteEvento.SelectedItem is not DataRowView drvAsist)
        {
            Aviso("Seleccione un evento y un asistente.");
            return;
        }
        int idEvento = Convert.ToInt32(drvEvento.Row["IdEvento"]);
        int idAsistente = Convert.ToInt32(drvAsist.Row["IdAsistente"]);
        Intentar(() =>
        {
            string respuesta = ProcedimientosBD.RegistrarAsistenteEvento(idAsistente, idEvento);
            Exito(respuesta);
            CargarDetalleEvento();
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

    private void CrearEventoParalelo()
    {
        if (string.IsNullOrWhiteSpace(txtNombreEvento.Text)) { Aviso("Ingrese el nombre del evento."); return; }
        if (cboTipoEvento.SelectedItem == null) { Aviso("Seleccione un tipo de evento."); return; }
        Intentar(() =>
        {
            string respuesta = ProcedimientosBD.CrearEventoParalelo(
                txtNombreEvento.Text.Trim(), cboTipoEvento.Text,
                dtpFechaEvento.Value, (int)nudAforoEvento.Value, nudCostoEvento.Value);
            Exito(respuesta);
            txtNombreEvento.Clear();
            cboTipoEvento.SelectedIndex = 0;
            dtpFechaEvento.Value = DateTime.Today.AddDays(1).AddHours(19);
            nudAforoEvento.Value = 1;
            nudCostoEvento.Value = 0;
            CargarEventosIniciales();
        });
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
        catch (SqlException)
        {
            // Error técnico de la BD (integridad, conexión, etc.)
            MessageBox.Show(this,
                "No fue posible completar la operación.",
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

    private static void EnlazarCheckedListBox(CheckedListBox clb, DataTable datos, string display, string valor)
    {
        clb.Items.Clear();
        foreach (DataRow r in datos.Rows)
        {
            string texto = r[display]?.ToString() ?? "";
            int id = Convert.ToInt32(r[valor]);
            clb.Items.Add(texto, false);
            // store IdCategoria in Tag for retrieval
            clb.Items[clb.Items.Count - 1] = new KeyValuePair<int, string>(id, texto);
        }
    }

    /// <summary>Agrega a la tabla una columna de texto descriptivo para mostrar en combos.</summary>
    private static void AgregarColumnaDescriptiva(DataTable dt, Func<DataRow, string> formato)
    {
        dt.Columns.Add("Descripcion_UI", typeof(string));
        foreach (DataRow fila in dt.Rows)
            fila["Descripcion_UI"] = formato(fila);
    }
}
