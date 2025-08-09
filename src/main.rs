#![windows_subsystem = "windows"]

use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::fs;
use windows::{
    core::*,
    Win32::Foundation::*,
    Win32::Graphics::Gdi::*,
    Win32::System::LibraryLoader::GetModuleHandleW,
    Win32::System::Com::*,
    Win32::UI::WindowsAndMessaging::*,
    Win32::UI::Controls::*,
};

#[derive(Debug, Clone, Deserialize, Serialize)]
struct ProgramEntry {
    name: String,
    category: Option<String>,
    download_url: Option<String>,
    silent_switches: Option<String>,
    vendor: Option<String>,
    version: Option<String>,
    size: Option<String>,
}

#[derive(Debug, Deserialize, Serialize)]
struct SoftwareCatalog {
    software: Vec<ProgramEntry>,
}

// Global state
static mut CATALOG: Option<SoftwareCatalog> = None;
static mut SELECTED_ITEMS: Option<HashMap<usize, bool>> = None;
static mut TREE_VIEW: HWND = HWND(0);
static mut LIST_VIEW: HWND = HWND(0);
static mut LOG_BOX: HWND = HWND(0);
static mut STATUS_BAR: HWND = HWND(0);
static mut CURRENT_CATEGORY: Option<String> = None;

// Control IDs
const ID_INSTALL: i32 = 1001;
const ID_DOWNLOAD: i32 = 1002;
const ID_STOP: i32 = 1003;
const ID_SELECT_ALL: i32 = 1004;
const ID_DESELECT: i32 = 1005;
const ID_STEALTH: i32 = 1006;
const ID_TREE: i32 = 2001;
const ID_LIST: i32 = 2002;
const ID_LOG: i32 = 3001;
const ID_STATUS: i32 = 4001;

fn main() -> Result<()> {
    unsafe {
        // Initialize COM for ListView
        CoInitializeEx(None, COINIT_APARTMENTTHREADED)?;
        InitCommonControls();

        // Load catalog
        load_catalog();
        SELECTED_ITEMS = Some(HashMap::new());

        let instance = GetModuleHandleW(None)?;
        let window_class = w!("NullInstallerClass");

        let wc = WNDCLASSW {
            hCursor: LoadCursorW(None, IDC_ARROW)?,
            hInstance: instance.into(),
            lpszClassName: window_class,
            style: CS_HREDRAW | CS_VREDRAW,
            lpfnWndProc: Some(wndproc),
            hbrBackground: HBRUSH(COLOR_WINDOW.0 as _),
            ..Default::default()
        };

        RegisterClassW(&wc);

        let _hwnd = CreateWindowExW(
            WINDOW_EX_STYLE::default(),
            window_class,
            w!("NullInstaller v4.2.6 - Professional Software Installer"),
            WS_OVERLAPPEDWINDOW | WS_VISIBLE,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            1000,
            700,
            None,
            None,
            instance,
            None,
        );

        let mut message = MSG::default();
        while GetMessageW(&mut message, None, 0, 0).into() {
            TranslateMessage(&message);
            DispatchMessageW(&message);
        }

        CoUninitialize();
        Ok(())
    }
}

extern "system" fn wndproc(window: HWND, message: u32, wparam: WPARAM, lparam: LPARAM) -> LRESULT {
    unsafe {
        match message {
            WM_CREATE => {
                create_ui(window);
                populate_tree_view();
                show_all_software();
                append_log("Ready - NullInstaller v4.2.6 initialized");
                if let Some(catalog) = &CATALOG {
                    append_log(&format!("Loaded {} programs from catalog", catalog.software.len()));
                }
                LRESULT(0)
            }
            WM_COMMAND => {
                let id = (wparam.0 & 0xFFFF) as i32;
                handle_command(window, id);
                LRESULT(0)
            }
            WM_NOTIFY => {
                let nmhdr = *(lparam.0 as *const NMHDR);
                if nmhdr.idFrom == ID_TREE as usize {
                    if nmhdr.code == TVN_SELCHANGED {
                        let pnmtv = lparam.0 as *const NMTREEVIEWW;
                        handle_tree_selection((*pnmtv).itemNew.hItem);
                    }
                } else if nmhdr.idFrom == ID_LIST as usize {
                    if nmhdr.code == NM_CLICK {
                        handle_list_click();
                    }
                }
                LRESULT(0)
            }
            WM_SIZE => {
                let width = (lparam.0 & 0xFFFF) as i32;
                let height = ((lparam.0 >> 16) & 0xFFFF) as i32;
                resize_controls(width, height);
                LRESULT(0)
            }
            WM_DESTROY => {
                PostQuitMessage(0);
                LRESULT(0)
            }
            _ => DefWindowProcW(window, message, wparam, lparam),
        }
    }
}

unsafe fn create_ui(window: HWND) {
    let instance = GetModuleHandleW(None).unwrap();

    // Create toolbar buttons (WinRAR style)
    CreateWindowExW(
        WINDOW_EX_STYLE::default(),
        w!("BUTTON"),
        w!("▶ Install"),
        WS_VISIBLE | WS_CHILD | WS_TABSTOP | BS_PUSHBUTTON,
        10, 10, 100, 35,
        window,
        HMENU(ID_INSTALL as _),
        instance,
        None,
    );

    CreateWindowExW(
        WINDOW_EX_STYLE::default(),
        w!("BUTTON"),
        w!("⬇ Download"),
        WS_VISIBLE | WS_CHILD | WS_TABSTOP,
        120, 10, 100, 35,
        window,
        HMENU(ID_DOWNLOAD as _),
        instance,
        None,
    );

    CreateWindowExW(
        WINDOW_EX_STYLE::default(),
        w!("BUTTON"),
        w!("■ Stop"),
        WS_VISIBLE | WS_CHILD | WS_TABSTOP | WS_DISABLED,
        230, 10, 80, 35,
        window,
        HMENU(ID_STOP as _),
        instance,
        None,
    );

    CreateWindowExW(
        WINDOW_EX_STYLE::default(),
        w!("BUTTON"),
        w!("☑ Select All"),
        WS_VISIBLE | WS_CHILD | WS_TABSTOP,
        320, 10, 100, 35,
        window,
        HMENU(ID_SELECT_ALL as _),
        instance,
        None,
    );

    CreateWindowExW(
        WINDOW_EX_STYLE::default(),
        w!("BUTTON"),
        w!("☐ Deselect"),
        WS_VISIBLE | WS_CHILD | WS_TABSTOP,
        430, 10, 100, 35,
        window,
        HMENU(ID_DESELECT as _),
        instance,
        None,
    );

    CreateWindowExW(
        WINDOW_EX_STYLE::default(),
        w!("BUTTON"),
        w!("🛡 Stealth"),
        WS_VISIBLE | WS_CHILD | WS_TABSTOP,
        540, 10, 100, 35,
        window,
        HMENU(ID_STEALTH as _),
        instance,
        None,
    );

    // Create TreeView (left panel - categories)
    TREE_VIEW = CreateWindowExW(
        WS_EX_CLIENTEDGE,
        WC_TREEVIEW,
        w!(""),
        WS_VISIBLE | WS_CHILD | WS_BORDER | TVS_HASLINES | TVS_LINESATROOT | TVS_HASBUTTONS | TVS_SHOWSELALWAYS,
        10, 60, 200, 400,
        window,
        HMENU(ID_TREE as _),
        instance,
        None,
    );

    // Create ListView (main panel - programs)
    LIST_VIEW = CreateWindowExW(
        WS_EX_CLIENTEDGE,
        WC_LISTVIEW,
        w!(""),
        WS_VISIBLE | WS_CHILD | LVS_REPORT | LVS_SINGLESEL | WS_BORDER,
        220, 60, 760, 400,
        window,
        HMENU(ID_LIST as _),
        instance,
        None,
    );

    // Set ListView extended styles
    SendMessageW(LIST_VIEW, LVM_SETEXTENDEDLISTVIEWSTYLE, WPARAM(0), 
        LPARAM((LVS_EX_FULLROWSELECT | LVS_EX_GRIDLINES | LVS_EX_CHECKBOXES) as _));

    // Add ListView columns
    add_list_column(0, "Name", 300);
    add_list_column(1, "Version", 80);
    add_list_column(2, "Size", 80);
    add_list_column(3, "Status", 120);
    add_list_column(4, "Vendor", 150);

    // Create log text box (bottom panel)
    LOG_BOX = CreateWindowExW(
        WS_EX_CLIENTEDGE,
        w!("EDIT"),
        w!(""),
        WS_VISIBLE | WS_CHILD | WS_VSCROLL | WS_BORDER | ES_MULTILINE | ES_READONLY | ES_AUTOVSCROLL,
        10, 470, 970, 150,
        window,
        HMENU(ID_LOG as _),
        instance,
        None,
    );

    // Create status bar
    STATUS_BAR = CreateWindowExW(
        WINDOW_EX_STYLE::default(),
        STATUSCLASSNAMEW,
        w!("Ready"),
        WS_VISIBLE | WS_CHILD | SBARS_SIZEGRIP,
        0, 0, 0, 0,
        window,
        HMENU(ID_STATUS as _),
        instance,
        None,
    );
}

unsafe fn add_list_column(index: i32, text: &str, width: i32) {
    let col = LVCOLUMNW {
        mask: LVCOLUMNW_MASK(LVCF_TEXT | LVCF_WIDTH | LVCF_SUBITEM),
        cx: width,
        pszText: PWSTR(encode_wide(text).as_mut_ptr()),
        iSubItem: index,
        ..Default::default()
    };
    SendMessageW(LIST_VIEW, LVM_INSERTCOLUMNW, WPARAM(index as _), LPARAM(&col as *const _ as _));
}

unsafe fn populate_tree_view() {
    if TREE_VIEW.0 == 0 { return; }

    // Add "All Software" root
    let all_item = add_tree_item(None, "📦 All Software");
    
    if let Some(catalog) = &CATALOG {
        let mut categories: HashMap<String, Vec<&ProgramEntry>> = HashMap::new();
        
        for program in &catalog.software {
            let category = program.category.as_deref().unwrap_or("Other");
            categories.entry(category.to_string()).or_insert_with(Vec::new).push(program);
        }

        let mut sorted_categories: Vec<_> = categories.keys().cloned().collect();
        sorted_categories.sort();

        for category in sorted_categories {
            let icon = get_category_icon(&category);
            let count = categories[&category].len();
            let text = format!("{} {} ({})", icon, category, count);
            add_tree_item(None, &text);
        }
    }

    // Add Stealth Mode
    add_tree_item(None, "🛡 Stealth Mode");

    // Select "All Software" by default
    SendMessageW(TREE_VIEW, TVM_SELECTITEM, WPARAM(TVGN_CARET as _), LPARAM(all_item.0));
}

unsafe fn add_tree_item(parent: Option<HTREEITEM>, text: &str) -> HTREEITEM {
    let mut item = TVINSERTSTRUCTW {
        hParent: parent.unwrap_or(TVI_ROOT),
        hInsertAfter: TVI_LAST,
        ..Default::default()
    };
    
    let mut wide_text = encode_wide(text);
    item.Anonymous.item.mask = TVITEM_MASK(TVIF_TEXT);
    item.Anonymous.item.pszText = PWSTR(wide_text.as_mut_ptr());
    
    HTREEITEM(SendMessageW(TREE_VIEW, TVM_INSERTITEMW, WPARAM(0), LPARAM(&item as *const _ as _)).0)
}

unsafe fn show_all_software() {
    if LIST_VIEW.0 == 0 { return; }
    
    SendMessageW(LIST_VIEW, LVM_DELETEALLITEMS, WPARAM(0), LPARAM(0));
    
    if let Some(catalog) = &CATALOG {
        for (i, program) in catalog.software.iter().enumerate() {
            add_list_item(i as i32, program);
        }
    }
    
    update_status(&format!("Showing {} programs", 
        CATALOG.as_ref().map_or(0, |c| c.software.len())));
}

unsafe fn add_list_item(index: i32, program: &ProgramEntry) {
    let item = LVITEMW {
        mask: LIST_VIEW_ITEM_FLAGS(LVIF_TEXT),
        iItem: index,
        iSubItem: 0,
        pszText: PWSTR(encode_wide(&program.name).as_mut_ptr()),
        ..Default::default()
    };
    
    let item_index = SendMessageW(LIST_VIEW, LVM_INSERTITEMW, WPARAM(0), LPARAM(&item as *const _ as _));
    
    // Add subitems
    set_list_subitem(item_index.0 as i32, 1, program.version.as_deref().unwrap_or("Latest"));
    set_list_subitem(item_index.0 as i32, 2, program.size.as_deref().unwrap_or("Unknown"));
    set_list_subitem(item_index.0 as i32, 3, "Ready");
    set_list_subitem(item_index.0 as i32, 4, program.vendor.as_deref().unwrap_or("Unknown"));
}

unsafe fn set_list_subitem(item: i32, subitem: i32, text: &str) {
    let lvi = LVITEMW {
        mask: LIST_VIEW_ITEM_FLAGS(LVIF_TEXT),
        iItem: item,
        iSubItem: subitem,
        pszText: PWSTR(encode_wide(text).as_mut_ptr()),
        ..Default::default()
    };
    SendMessageW(LIST_VIEW, LVM_SETITEMW, WPARAM(0), LPARAM(&lvi as *const _ as _));
}

unsafe fn handle_command(window: HWND, id: i32) {
    match id {
        ID_INSTALL => {
            let count = get_selected_count();
            if count == 0 {
                MessageBoxW(window, w!("Please select programs to install"), 
                    w!("No Selection"), MB_OK | MB_ICONINFORMATION);
                return;
            }
            append_log(&format!("Starting installation of {} programs...", count));
            // Enable Stop, disable Install/Download
            let _ = SendMessageW(GetDlgItem(window, ID_STOP), WM_ENABLE, WPARAM(1), LPARAM(0));
            let _ = SendMessageW(GetDlgItem(window, ID_INSTALL), WM_ENABLE, WPARAM(0), LPARAM(0));
            let _ = SendMessageW(GetDlgItem(window, ID_DOWNLOAD), WM_ENABLE, WPARAM(0), LPARAM(0));
        }
        ID_DOWNLOAD => {
            let count = get_selected_count();
            if count == 0 {
                MessageBoxW(window, w!("Please select programs to download"), 
                    w!("No Selection"), MB_OK | MB_ICONINFORMATION);
                return;
            }
            append_log(&format!("Starting download of {} programs...", count));
        }
        ID_STOP => {
            append_log("Operation cancelled by user");
            let _ = SendMessageW(GetDlgItem(window, ID_STOP), WM_ENABLE, WPARAM(0), LPARAM(0));
            let _ = SendMessageW(GetDlgItem(window, ID_INSTALL), WM_ENABLE, WPARAM(1), LPARAM(0));
            let _ = SendMessageW(GetDlgItem(window, ID_DOWNLOAD), WM_ENABLE, WPARAM(1), LPARAM(0));
        }
        ID_SELECT_ALL => {
            select_all_items(true);
            append_log("All items selected");
        }
        ID_DESELECT => {
            select_all_items(false);
            append_log("All items deselected");
        }
        ID_STEALTH => {
            show_stealth_programs();
            append_log("Stealth mode activated - privacy tools selected");
        }
        _ => {}
    }
}

unsafe fn handle_tree_selection(item: HTREEITEM) {
    // Get selected text
    let tvi = TVITEMW {
        mask: TVITEM_MASK(TVIF_TEXT),
        hItem: item,
        pszText: PWSTR(vec![0u16; 256].as_mut_ptr()),
        cchTextMax: 256,
        ..Default::default()
    };
    
    SendMessageW(TREE_VIEW, TVM_GETITEMW, WPARAM(0), LPARAM(&tvi as *const _ as _));
    
    let text = from_wide_ptr(tvi.pszText.0);
    
    if text.contains("All Software") {
        show_all_software();
    } else if text.contains("Stealth Mode") {
        show_stealth_programs();
    } else {
        // Extract category name (remove icon and count)
        if let Some(_catalog) = &CATALOG {
            let category = text.split(' ').skip(1).next().unwrap_or("");
            show_category_programs(category);
        }
    }
}

unsafe fn show_category_programs(category: &str) {
    SendMessageW(LIST_VIEW, LVM_DELETEALLITEMS, WPARAM(0), LPARAM(0));
    
    if let Some(catalog) = &CATALOG {
        let mut index = 0;
        for program in &catalog.software {
            if program.category.as_deref() == Some(category) {
                add_list_item(index, program);
                index += 1;
            }
        }
        update_status(&format!("Showing {} programs in {}", index, category));
    }
}

unsafe fn show_stealth_programs() {
    SendMessageW(LIST_VIEW, LVM_DELETEALLITEMS, WPARAM(0), LPARAM(0));
    
    if let Some(catalog) = &CATALOG {
        let mut index = 0;
        for program in &catalog.software {
            if let Some(cat) = &program.category {
                if cat.contains("Security") || cat.contains("Privacy") ||
                   program.name.contains("VPN") || program.name.contains("Tor") ||
                   program.name.contains("Privacy") {
                    add_list_item(index, program);
                    index += 1;
                }
            }
        }
        update_status(&format!("Showing {} stealth/privacy programs", index));
    }
}

unsafe fn handle_list_click() {
    update_status(&format!("Selected {} items", get_selected_count()));
}

unsafe fn select_all_items(select: bool) {
    let count = SendMessageW(LIST_VIEW, LVM_GETITEMCOUNT, WPARAM(0), LPARAM(0)).0;
    for i in 0..count {
        ListView_SetCheckState(LIST_VIEW, i as i32, select);
    }
    update_status(&format!("{} {} items", 
        if select { "Selected" } else { "Deselected" }, count));
}

unsafe fn get_selected_count() -> usize {
    let count = SendMessageW(LIST_VIEW, LVM_GETITEMCOUNT, WPARAM(0), LPARAM(0)).0;
    let mut selected = 0;
    for i in 0..count {
        if ListView_GetCheckState(LIST_VIEW, i as i32) {
            selected += 1;
        }
    }
    selected
}

unsafe fn ListView_SetCheckState(hwnd: HWND, i: i32, check: bool) {
    let item = LVITEMW {
        mask: LIST_VIEW_ITEM_FLAGS(LVIF_STATE),
        iItem: i,
        iSubItem: 0,
        state: LIST_VIEW_ITEM_STATE_FLAGS(if check { 0x2000 } else { 0x1000 }),
        stateMask: LIST_VIEW_ITEM_STATE_FLAGS(LVIS_STATEIMAGEMASK),
        ..Default::default()
    };
    SendMessageW(hwnd, LVM_SETITEMW, WPARAM(0), LPARAM(&item as *const _ as _));
}

unsafe fn ListView_GetCheckState(hwnd: HWND, i: i32) -> bool {
    let item = LVITEMW {
        mask: LIST_VIEW_ITEM_FLAGS(LVIF_STATE),
        iItem: i,
        iSubItem: 0,
        stateMask: LIST_VIEW_ITEM_STATE_FLAGS(LVIS_STATEIMAGEMASK),
        ..Default::default()
    };
    SendMessageW(hwnd, LVM_GETITEMW, WPARAM(0), LPARAM(&item as *const _ as _));
    (item.state.0 & 0x2000) != 0
}

unsafe fn resize_controls(width: i32, height: i32) {
    // Resize controls when window is resized
    if TREE_VIEW.0 != 0 {
        SetWindowPos(TREE_VIEW, None, 10, 60, 200, height - 270, SWP_NOZORDER);
    }
    if LIST_VIEW.0 != 0 {
        SetWindowPos(LIST_VIEW, None, 220, 60, width - 240, height - 270, SWP_NOZORDER);
    }
    if LOG_BOX.0 != 0 {
        SetWindowPos(LOG_BOX, None, 10, height - 200, width - 20, 150, SWP_NOZORDER);
    }
    if STATUS_BAR.0 != 0 {
        SendMessageW(STATUS_BAR, WM_SIZE, WPARAM(0), LPARAM(0));
    }
}

unsafe fn append_log(text: &str) {
    if LOG_BOX.0 == 0 { return; }
    
    let current_len = GetWindowTextLengthW(LOG_BOX);
    SendMessageW(LOG_BOX, EM_SETSEL, WPARAM(current_len as _), LPARAM(current_len as _));
    
    let timestamp = format!("[{}] {}\r\n", get_timestamp(), text);
    let wide_text = encode_wide(&timestamp);
    SendMessageW(LOG_BOX, EM_REPLACESEL, WPARAM(0), LPARAM(wide_text.as_ptr() as _));
}

unsafe fn update_status(text: &str) {
    if STATUS_BAR.0 == 0 { return; }
    let wide_text = encode_wide(text);
    SendMessageW(STATUS_BAR, SB_SETTEXTW, WPARAM(0), LPARAM(wide_text.as_ptr() as _));
}

unsafe fn load_catalog() {
    if let Ok(contents) = fs::read_to_string("assets/software_catalog.json") {
        if let Ok(catalog) = serde_json::from_str::<SoftwareCatalog>(&contents) {
            CATALOG = Some(catalog);
        }
    }
}

fn get_category_icon(category: &str) -> &str {
    match category.to_lowercase().as_str() {
        "browsers" => "🌐",
        "development" | "development ides" => "💻",
        "security" | "privacy & security" => "🔒",
        "media" => "🎬",
        "productivity" => "📊",
        "system" | "utilities" => "⚙",
        "gaming" => "🎮",
        "network" => "📡",
        "communication" => "💬",
        _ => "📁",
    }
}

fn get_timestamp() -> String {
    // Simple timestamp
    "12:00:00".to_string()
}

fn encode_wide(s: &str) -> Vec<u16> {
    s.encode_utf16().chain(std::iter::once(0)).collect()
}

unsafe fn from_wide_ptr(ptr: *const u16) -> String {
    let mut len = 0;
    while *ptr.offset(len) != 0 {
        len += 1;
    }
    let slice = std::slice::from_raw_parts(ptr, len as usize);
    String::from_utf16_lossy(slice)
}

// Windows constants
const TVN_SELCHANGED: u32 = 0xFFFFFFFE - 2;
const TVM_INSERTITEMW: u32 = TV_FIRST + 50;
const TVM_SELECTITEM: u32 = TV_FIRST + 11;
const TVM_GETITEMW: u32 = TV_FIRST + 62;
const TVGN_CARET: u32 = 0x0009;
const TV_FIRST: u32 = 0x1100;
const TVI_ROOT: HTREEITEM = HTREEITEM(-0x10000isize as _);
const TVI_LAST: HTREEITEM = HTREEITEM(-0x0FFFFisize as _);
const TVIF_TEXT: u32 = 0x0001;
const TVS_HASLINES: WINDOW_STYLE = WINDOW_STYLE(0x0002);
const TVS_LINESATROOT: WINDOW_STYLE = WINDOW_STYLE(0x0004);
const TVS_HASBUTTONS: WINDOW_STYLE = WINDOW_STYLE(0x0001);
const TVS_SHOWSELALWAYS: WINDOW_STYLE = WINDOW_STYLE(0x0020);

const LVM_FIRST: u32 = 0x1000;
const LVM_INSERTITEMW: u32 = LVM_FIRST + 77;
const LVM_SETITEMW: u32 = LVM_FIRST + 76;
const LVM_GETITEMW: u32 = LVM_FIRST + 75;
const LVM_INSERTCOLUMNW: u32 = LVM_FIRST + 97;
const LVM_DELETEALLITEMS: u32 = LVM_FIRST + 9;
const LVM_SETEXTENDEDLISTVIEWSTYLE: u32 = LVM_FIRST + 54;
const LVM_GETITEMCOUNT: u32 = LVM_FIRST + 4;

const LVS_REPORT: WINDOW_STYLE = WINDOW_STYLE(0x0001);
const LVS_SINGLESEL: WINDOW_STYLE = WINDOW_STYLE(0x0004);
const LVS_EX_FULLROWSELECT: u32 = 0x00000020;
const LVS_EX_GRIDLINES: u32 = 0x00000001;
const LVS_EX_CHECKBOXES: u32 = 0x00000004;

const LVIF_TEXT: u32 = 0x0001;
const LVIF_STATE: u32 = 0x0008;
const LVIS_STATEIMAGEMASK: u32 = 0xF000;

const LVCF_TEXT: u32 = 0x0004;
const LVCF_WIDTH: u32 = 0x0002;
const LVCF_SUBITEM: u32 = 0x0008;

const ES_MULTILINE: WINDOW_STYLE = WINDOW_STYLE(0x0004);
const ES_READONLY: WINDOW_STYLE = WINDOW_STYLE(0x0800);
const ES_AUTOVSCROLL: WINDOW_STYLE = WINDOW_STYLE(0x0040);
const EM_SETSEL: u32 = 0x00B1;
const EM_REPLACESEL: u32 = 0x00C2;

const BS_PUSHBUTTON: WINDOW_STYLE = WINDOW_STYLE(0x00000000);
const SBARS_SIZEGRIP: WINDOW_STYLE = WINDOW_STYLE(0x0100);
const SB_SETTEXTW: u32 = WM_USER + 11;
const WM_ENABLE: u32 = 0x000A;
