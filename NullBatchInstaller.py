import os
import sys
import time
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor
from PyQt6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QVBoxLayout, QHBoxLayout,
    QPushButton, QLabel, QProgressBar, QTreeWidget, QTreeWidgetItem,
    QTextEdit, QSplitter, QFileDialog, QMessageBox, QStyleFactory
)
from PyQt6.QtCore import Qt, QThread, pyqtSignal
from PyQt6.QtGui import QDragEnterEvent, QDropEvent, QPalette, QColor
import zipfile
import py7zr
import subprocess

class SystemMonitor(QThread):
    metrics_updated = pyqtSignal(dict)
    
    def __init__(self):
        super().__init__()
        self.running = True

    def get_metrics(self):
        metrics = {}
        try:
            if sys.platform == "win32":
                cmd = "wmic cpu get loadpercentage"
                cpu = float(subprocess.check_output(cmd).decode().split("\n")[1])
                metrics['cpu'] = cpu
                
                cmd = "wmic OS get FreePhysicalMemory,TotalVisibleMemorySize /Value"
                output = subprocess.check_output(cmd).decode()
                total = int(output.split('=')[2])
                free = int(output.split('=')[1])
                memory_percent = ((total - free) / total) * 100
                metrics['memory'] = memory_percent
            else:
                cmd = "top -bn1"
                output = subprocess.check_output(cmd.split()).decode()
                cpu = float(output.split('\n')[2].split()[1])
                metrics['cpu'] = cpu
                
                cmd = "free"
                output = subprocess.check_output(cmd.split()).decode()
                memory_percent = float(output.split('\n')[1].split()[2])
                metrics['memory'] = memory_percent
                
            metrics['disk'] = self._get_disk_usage()
        except Exception as e:
            print(f"Error collecting metrics: {str(e)}")
            metrics = {'cpu': 0, 'memory': 0, 'disk': 0}
        return metrics

    def _get_disk_usage(self):
        if sys.platform == "win32":
            cmd = "wmic logicaldisk get size,freespace"
            output = subprocess.check_output(cmd).decode()
            total = int(output.split('\n')[1].split()[0])
            free = int(output.split('\n')[1].split()[1])
            return ((total - free) / total) * 100
        else:
            cmd = "df /"
            output = subprocess.check_output(cmd.split()).decode()
            return float(output.split('\n')[1].split()[4].strip('%'))

    def run(self):
        while self.running:
            try:
                metrics = self.get_metrics()
                self.metrics_updated.emit(metrics)
                time.sleep(1)
            except Exception as e:
                print(f"Error collecting metrics: {str(e)}")

    def stop(self):
        self.running = False

class BatchInstaller(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("NullBatchInstaller v1.2.2")
        self.setGeometry(100, 100, 900, 600)
        self._init_variables()
        self._setup_ui()
        self._setup_theme()
        self.setAcceptDrops(True)

    def _init_variables(self):
        self.files_to_install = []
        self.installation_mode = "silent"
        self.is_installing = False
        self.start_time = None
        self.system_monitor = SystemMonitor()
        self.system_monitor.metrics_updated.connect(self._update_system_metrics)

    def _setup_ui(self):
        central_widget = QWidget()
        self.setCentralWidget(central_widget)
        layout = QVBoxLayout(central_widget)
        layout.setContentsMargins(10, 10, 10, 10)
        layout.setSpacing(5)

        # Create splitter
        splitter = QSplitter(Qt.Orientation.Horizontal)
        splitter.setChildrenCollapsible(False)

        # File list
        self.file_list = QTreeWidget()
        self.file_list.setHeaderLabels(["File", "Status", "Progress", "Size"])
        self.file_list.setMinimumHeight(400)
        splitter.addWidget(self.file_list)

        # Log viewer
        self.log_text = QTextEdit()
        self.log_text.setReadOnly(True)
        self.log_text.setMinimumHeight(400)
        splitter.addWidget(self.log_text)

        # Set equal sizes for both panels
        splitter.setSizes([1, 1])
        layout.addWidget(splitter)

        # Controls
        controls_layout = QHBoxLayout()
        controls_layout.setContentsMargins(0, 5, 0, 5)

        # Add Files button
        self.add_files_button = QPushButton("Add Files")
        self.add_files_button.clicked.connect(self.browse_files)
        controls_layout.addWidget(self.add_files_button)

        self.start_button = QPushButton("Start")
        self.start_button.clicked.connect(self.start_installation)
        self.stop_button = QPushButton("Stop")
        self.stop_button.clicked.connect(self.stop_installation)
        self.stop_button.setEnabled(False)
        self.clear_button = QPushButton("Clear")
        self.clear_button.clicked.connect(self.clear_list)
        self.progress_bar = QProgressBar()

        # System monitoring labels
        self.cpu_label = QLabel("CPU: 0%")
        self.memory_label = QLabel("Memory: 0%")
        self.disk_label = QLabel("Disk: 0%")

        controls_layout.addWidget(self.start_button)
        controls_layout.addWidget(self.stop_button)
        controls_layout.addWidget(self.clear_button)
        controls_layout.addWidget(self.progress_bar)
        controls_layout.addWidget(self.cpu_label)
        controls_layout.addWidget(self.memory_label)
        controls_layout.addWidget(self.disk_label)

        layout.addLayout(controls_layout)

    def _setup_theme(self):
        self.setStyle(QStyleFactory.create('Fusion'))
        palette = QPalette()
        palette.setColor(QPalette.ColorRole.Window, QColor("#2E1A47"))
        palette.setColor(QPalette.ColorRole.WindowText, Qt.GlobalColor.white)
        palette.setColor(QPalette.ColorRole.Base, QColor("#3E2A57"))
        palette.setColor(QPalette.ColorRole.Text, Qt.GlobalColor.white)
        palette.setColor(QPalette.ColorRole.Button, QColor("#8A2BE2"))
        palette.setColor(QPalette.ColorRole.ButtonText, Qt.GlobalColor.white)
        self.setPalette(palette)

    def browse_files(self):
        files, _ = QFileDialog.getOpenFileNames(
            self,
            "Select Files to Install",
            "",
            "Installers (*.exe *.msi *.zip *.7z);;All Files (*.*)"
        )
        for file_path in files:
            self.add_file(file_path)

    def _update_system_metrics(self, metrics):
        self.cpu_label.setText(f"CPU: {metrics['cpu']:.1f}%")
        self.memory_label.setText(f"Memory: {metrics['memory']:.1f}%")
        self.disk_label.setText(f"Disk: {metrics['disk']:.1f}%")

    def dragEnterEvent(self, event: QDragEnterEvent):
        if event.mimeData().hasUrls():
            event.acceptProposedAction()

    def dropEvent(self, event: QDropEvent):
        for url in event.mimeData().urls():
            file_path = url.toLocalFile()
            self.add_file(file_path)

    def add_file(self, file_path):
        if not os.path.exists(file_path):
            self.log(f"Error: File not found - {file_path}")
            return

        if file_path.lower().endswith(('.zip', '.7z')):
            self.unzip_file(file_path)
            return

        file_info = {
            'path': file_path,
            'status': 'Pending',
            'progress': '0%'
        }

        size = os.path.getsize(file_path) / (1024 * 1024)  # Convert to MB
        item = QTreeWidgetItem([
            os.path.basename(file_path),
            'Pending',
            '0%',
            f"{size:.1f} MB"
        ])
        self.file_list.addTopLevelItem(item)
        self.files_to_install.append(file_info)
        self.log(f"Added: {os.path.basename(file_path)}")

    def unzip_file(self, file_path):
        try:
            extract_dir = os.path.dirname(file_path)
            if file_path.endswith('.zip'):
                with zipfile.ZipFile(file_path, 'r') as zip_ref:
                    zip_ref.extractall(extract_dir)
            elif file_path.endswith('.7z'):
                with py7zr.SevenZipFile(file_path, mode='r') as archive:
                    archive.extractall(path=extract_dir)
            self.log(f"Successfully extracted: {file_path}")
            self._add_executables_from_dir(extract_dir)
        except Exception as e:
            self.log(f"Error extracting {file_path}: {str(e)}")

    def _add_executables_from_dir(self, directory):
        for root, _, files in os.walk(directory):
            for file in files:
                if file.lower().endswith(('.exe', '.msi')):
                    self.add_file(os.path.join(root, file))

    def start_installation(self):
        if not self.files_to_install:
            QMessageBox.warning(self, "No Files", "Please add files to install first.")
            return

        self.is_installing = True
        self.start_time = time.time()
        self.start_button.setEnabled(False)
        self.stop_button.setEnabled(True)
        self.system_monitor.start()

        self.installation_thread = ThreadPoolExecutor(max_workers=1)
        self.installation_thread.submit(self.run_installations)

    def run_installations(self):
        total_files = len(self.files_to_install)
        completed = 0

        for file_info in self.files_to_install:
            if not self.is_installing:
                break

            file_path = file_info['path']
            try:
                self.update_file_status(file_path, "Installing", "0%")
                cmd = [file_path, "/quiet", "/norestart"] if self.installation_mode == "silent" else [file_path]
                process = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, universal_newlines=True)
                
                while process.poll() is None and self.is_installing:
                    time.sleep(1)

                if process.returncode == 0:
                    self.update_file_status(file_path, "Completed", "100%")
                    self.log(f"Successfully installed: {os.path.basename(file_path)}")
                else:
                    self.update_file_status(file_path, "Failed", "0%")
                    self.log(f"Installation failed for {os.path.basename(file_path)}")
            except Exception as e:
                self.update_file_status(file_path, "Failed", "0%")
                self.log(f"Error installing {os.path.basename(file_path)}: {str(e)}")

            completed += 1
            self.progress_bar.setValue(int((completed / total_files) * 100))

        self.is_installing = False
        self.start_button.setEnabled(True)
        self.stop_button.setEnabled(False)
        self.system_monitor.stop()
        self.log("Installation process completed")

    def update_file_status(self, file_path, status, progress):
        for i in range(self.file_list.topLevelItemCount()):
            item = self.file_list.topLevelItem(i)
            if item.text(0) == os.path.basename(file_path):
                item.setText(1, status)
                item.setText(2, progress)
                break

    def stop_installation(self):
        self.is_installing = False
        self.start_button.setEnabled(True)
        self.stop_button.setEnabled(False)
        self.system_monitor.stop()
        self.log("Installation stopped by user")

    def clear_list(self):
        self.file_list.clear()
        self.files_to_install.clear()
        self.log("Cleared installation list")

    def log(self, message):
        timestamp = datetime.now().strftime("%H:%M:%S")
        self.log_text.append(f"[{timestamp}] {message}")
        with open("install_log.txt", "a") as log_file:
            log_file.write(f"[{timestamp}] {message}\n")

if __name__ == "__main__":
    try:
        app = QApplication(sys.argv)
        app.setStyle('Fusion')
        window = BatchInstaller()
        window.show()
        sys.exit(app.exec())
    except Exception as e:
        print(f"Error starting application: {str(e)}")
        input("Press Enter to exit...")
