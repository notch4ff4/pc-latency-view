import os
import sys
import csv
import statistics
from PyQt6.QtWidgets import (QApplication, QMainWindow, QWidget, QVBoxLayout,
                            QLabel, QListWidget, QMessageBox, QFrame)
from PyQt6.QtCore import Qt
from PyQt6.QtGui import QFont

class MsPCLatencyAnalyzer(QMainWindow):
    def __init__(self):
        super().__init__()

        # Настройка главного окна
        self.setWindowTitle("Анализ PC Latency")
        self.setFixedSize(500, 600)

        # Создание центрального виджета
        central_widget = QWidget()
        self.setCentralWidget(central_widget)

        # Создание главного layout
        main_layout = QVBoxLayout(central_widget)

        # Создание фреймов для файлов и результатов
        file_frame = QFrame()
        file_frame.setFrameStyle(QFrame.Shape.Box | QFrame.Shadow.Raised)
        result_frame = QFrame()
        result_frame.setFrameStyle(QFrame.Shape.Box | QFrame.Shadow.Raised)

        # Layouts для фреймов
        file_layout = QVBoxLayout(file_frame)
        result_layout = QVBoxLayout(result_frame)

        # Настройка секции файлов
        file_label = QLabel("Файлы CSV")
        file_label.setFont(QFont("Arial", 16, QFont.Weight.Bold))
        file_layout.addWidget(file_label, alignment=Qt.AlignmentFlag.AlignCenter)

        # Список файлов
        self.file_list = QListWidget()
        self.file_list.itemClicked.connect(self.on_file_select)
        file_layout.addWidget(self.file_list)

        # Настройка секции результатов
        result_label = QLabel("Результаты PC Latency")
        result_label.setFont(QFont("Arial", 16, QFont.Weight.Bold))
        result_layout.addWidget(result_label, alignment=Qt.AlignmentFlag.AlignCenter)

        self.min_label = QLabel("Минимум: -")
        self.min_label.setFont(QFont("Arial", 14))
        self.mean_label = QLabel("Среднее: -")
        self.mean_label.setFont(QFont("Arial", 14))
        self.max_label = QLabel("Максимум: -")
        self.max_label.setFont(QFont("Arial", 14))

        result_layout.addWidget(self.min_label, alignment=Qt.AlignmentFlag.AlignCenter)
        result_layout.addWidget(self.mean_label, alignment=Qt.AlignmentFlag.AlignCenter)
        result_layout.addWidget(self.max_label, alignment=Qt.AlignmentFlag.AlignCenter)

        # Добавление фреймов в главный layout
        main_layout.addWidget(file_frame)
        main_layout.addWidget(result_frame)

        # Загрузка CSV файлов
        self.load_csv_files()

    def get_base_path(self):
        if getattr(sys, 'frozen', False):
            return os.path.dirname(sys.executable)
        else:
            return os.path.dirname(os.path.abspath(__file__))

    def load_csv_files(self):
        base_path = self.get_base_path()
        csv_files = [f for f in os.listdir(base_path) if f.endswith('.csv')]

        if not csv_files:
            QMessageBox.critical(self, "Ошибка", "В текущей директории нет CSV-файлов.")

        self.file_list.addItems(csv_files)

    def process_csv_file(self, file_name):
        base_path = self.get_base_path()
        file_path = os.path.join(base_path, file_name)

        try:
            with open(file_path, newline='', encoding='utf-8') as csvfile:
                reader = csv.DictReader(csvfile)

                # Проверка на наличие нужных столбцов
                if 'MsPCLatency' in reader.fieldnames:
                    latency_column = 'MsPCLatency'
                elif 'AllInputToPhotonLatency' in reader.fieldnames:
                    latency_column = 'AllInputToPhotonLatency'
                else:
                    QMessageBox.critical(
                        self,
                        "Ошибка",
                        f"Нет столбца 'MsPCLatency' или 'AllInputToPhotonLatency' в файле {file_name}."
                    )
                    return None

                values = []
                for row in reader:
                    try:
                        values.append(float(row[latency_column]))
                    except ValueError:
                        pass

                if not values:
                    QMessageBox.critical(
                        self,
                        "Ошибка",
                        f"Нет численных значений в столбце '{latency_column}'."
                    )
                    return None

                return {
                    "min": min(values),
                    "mean": statistics.mean(values),
                    "max": max(values)
                }
        except Exception as e:
            QMessageBox.critical(self, "Ошибка", f"Не удалось обработать файл {file_name}.\n{e}")
            return None

    def on_file_select(self, item):
        selected_file = item.text()
        result = self.process_csv_file(selected_file)
        if result:
            self.min_label.setText(f"Минимум: {result['min']:.2f} ms")
            self.mean_label.setText(f"Среднее: {result['mean']:.2f} ms")
            self.max_label.setText(f"Максимум: {result['max']:.2f} ms")


def main():
    app = QApplication(sys.argv)
    window = MsPCLatencyAnalyzer()
    window.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
