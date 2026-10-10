// Mở modal thêm/sửa: điền dữ liệu từ data-json vào các ô có name trùng khóa.
document.addEventListener('click', function (e) {
  var b = e.target.closest('[data-modal]');
  if (!b) return;
  var modal = document.querySelector(b.dataset.modal);
  if (!modal) return;
  var f = modal.querySelector('form');
  f.reset();
  var data = b.dataset.json ? JSON.parse(b.dataset.json) : {};
  f.querySelectorAll('[name]').forEach(function (el) {
    if (Object.prototype.hasOwnProperty.call(data, el.name) && data[el.name] !== null) el.value = data[el.name];
  });
  var t = modal.querySelector('.modal-title');
  if (t && b.dataset.title) t.textContent = b.dataset.title;
});

// Xác nhận trước khi xóa
document.addEventListener('submit', function (e) {
  var f = e.target;
  if (f.dataset.confirm && !window.confirm(f.dataset.confirm)) e.preventDefault();
});
