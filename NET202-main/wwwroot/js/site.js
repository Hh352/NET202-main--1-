// ====================================================
// 1. TẮT MỞ UI GIỎ HÀNG (SIDEBAR)
// ====================================================
function toggleCart() {
    const sidebar = document.getElementById("cartSidebar");
    const overlay = document.getElementById("overlay");

    if (sidebar && overlay) {
        sidebar.classList.toggle("active");
        overlay.classList.toggle("active");
    }
}

// ====================================================
// 2. THÊM SẢN PHẨM VÀO GIỎ HÀNG
// ====================================================
function addToCart(productId) {
    // isMainUserLoggedIn nên được khai báo biến global ở _Layout hoặc check từ token/cookie
    if (typeof isMainUserLoggedIn !== 'undefined' && !isMainUserLoggedIn) {
        Swal.fire({
            title: 'Yêu cầu đăng nhập',
            text: "Vui lòng đăng nhập để mua hàng tại Bread & Brew!",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d97706',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Đăng nhập ngay',
            cancelButtonText: 'Đóng'
        }).then((result) => {
            if (result.isConfirmed) window.location.href = '/Account/Login';
        });
        return;
    }

    fetch(`/Cart/AddToCart?id=${productId}`, { method: 'POST' })
    .then(res => {
        if (!res.ok) throw new Error("Lỗi kết nối server"); 
        return res.json();
    })
    .then(data => {
        if (data.success) {
            Swal.fire({
                title: 'Đã thêm vào giỏ!',
                icon: 'success',
                timer: 1000,
                showConfirmButton: false,
                position: 'top-end', 
                toast: true,
                background: '#fdf6f0',
                color: '#d97706'
            });

            // Load lại dữ liệu Sidebar và chấm đỏ Header
            loadCartSidebar(); 

            // Tự động mở Sidebar cho khách thấy nếu đang đóng
            const sidebar = document.getElementById("cartSidebar");
            if (sidebar && !sidebar.classList.contains("active")) {
                toggleCart();
            }
        }
    })
    .catch(err => {
        console.error("Lỗi:", err);
        Swal.fire({
            title: 'Lỗi!',
            text: 'Không thể thêm vào giỏ hàng, vui lòng thử lại!',
            icon: 'error',
            confirmButtonColor: '#d97706'
        });
    });
}

// ====================================================
// 3. THANH TOÁN (CHECKOUT)
// ====================================================
function checkout() {
    Swal.fire({
        title: 'Đang xử lý...',
        allowOutsideClick: false,
        didOpen: () => { Swal.showLoading(); }
    });

    fetch('/Cart/Checkout', {
        method: 'POST'
    })
    .then(res => res.json())
    .then(data => {
        if (data.success && data.orderId) {
            window.location.href = `/Order/Detail/${data.orderId}?isConfirm=true`;
        } else {
            Swal.fire('Lỗi thanh toán!', data.message || 'Hệ thống bận, vui lòng thử lại.', 'error');
        }
    })
    .catch(err => {
        console.error(err);
        Swal.fire('Lỗi kết nối!', 'Không thể liên lạc với máy chủ.', 'error');
    });
}

// ====================================================
// 4. LOAD DỮ LIỆU LÊN SIDEBAR & HEADER (RENDER HTML)
// ====================================================
function loadCartSidebar() {
    fetch('/Cart/GetCart')
    .then(res => res.json())
    .then(data => {
        const cartList = document.getElementById('cartList');
        const cartTotal = document.getElementById('cartTotal');
        const badge = document.querySelector('.cart-badge'); // Chấm đỏ trên Header

        if (!data.items || data.items.length === 0) {
            if(cartList) cartList.innerHTML = `<div class="empty-cart-msg" style="text-align:center; padding: 40px 20px; color:#a16207; font-weight:600;"><i class="fa-solid fa-basket-shopping" style="font-size:3rem; opacity:0.5; margin-bottom:10px; display:block;"></i>Giỏ hàng đang trống :(</div>`;
            if(cartTotal) cartTotal.innerText = '0đ';
            if(badge) badge.innerText = '0';
            return;
        } 

        let html = '';
        let totalItemsCount = 0; // Đếm tổng số lượng món (vd: 2 cafe + 1 bánh = 3)

        // RENDER DANH SÁCH MÓN
        data.items.forEach(item => {
            totalItemsCount += item.quantity; // Cộng dồn số lượng cho badge
            
            let imgSrc = (item.image && item.image.startsWith("http")) 
                ? item.image 
                : `/Images/${item.image || 'default.png'}`;

            html += `
                <div class="cart-item" style="display:flex; align-items:center; gap:15px; padding:15px 0; border-bottom:1px solid #f3f4f6;">
                    <img src="${imgSrc}" onerror="this.src='/Images/default.png'" style="width:60px; height:60px; border-radius:10px; object-fit:cover;" />
                    <div class="cart-item-info" style="flex:1;">
                        <h4 style="margin:0 0 5px 0; font-size:15px; font-weight:700; color:#4b2e2e;">${item.productName}</h4>
                        <div class="price" style="color:#d97706; font-weight:800; font-size:14px;">${item.price.toLocaleString()}đ</div>
                    </div>
                    <div class="cart-item-actions" style="display:flex; flex-direction:column; align-items:flex-end; gap:8px;">
                        <button class="btn-delete-item" onclick="removeItem(${item.productId})" style="background:transparent; border:none; color:#ef4444; cursor:pointer; font-size:12px; font-weight:bold;">
                            <i class="fa-solid fa-trash-can"></i> Xóa
                        </button>
                        <div class="cart-item-qty" style="display:flex; align-items:center; background:#fef3c7; border-radius:6px; overflow:hidden; border:1px solid #fde68a;">
                            <button class="qty-btn" onclick="updateQty(${item.productId}, 'Decrease')" style="border:none; background:transparent; width:26px; height:26px; color:#d97706; font-weight:bold; cursor:pointer;">−</button>
                            <div class="qty-val" style="width:24px; text-align:center; font-size:13px; font-weight:bold; color:#4b2e2e;">${item.quantity}</div>
                            <button class="qty-btn" onclick="updateQty(${item.productId}, 'Increase')" style="border:none; background:transparent; width:26px; height:26px; color:#d97706; font-weight:bold; cursor:pointer;">+</button>
                        </div>
                    </div>
                </div>
            `;
        });

        if(cartList) cartList.innerHTML = html;
        
        // Cập nhật chấm đỏ trên thanh Header
        if(badge) badge.innerText = totalItemsCount;
        
        // HIỂN THỊ TỔNG TIỀN VÀ TRỪ TIỀN VOUCHER
        if(cartTotal) {
            if (data.discount > 0) {
                cartTotal.innerHTML = `<del style="color:#94a3b8; font-size:14px; margin-right:8px; font-weight:normal;">${data.total.toLocaleString()}đ</del> <span style="color:#d97706;">${data.finalTotal.toLocaleString()}đ</span>`;
            } else {
                cartTotal.innerText = data.finalTotal.toLocaleString() + 'đ';
            }
        }
    })
    .catch(err => {
        console.error("Lỗi khi load giỏ hàng:", err);
    });
}

// ====================================================
// 5. CẬP NHẬT & XÓA SẢN PHẨM TRONG GIỎ HÀNG
// ====================================================
function updateQty(id, action) {
    fetch(`/Cart/${action}?id=${id}`, { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            if (data.success) loadCartSidebar();
        });
}

function removeItem(id) {
    Swal.fire({
        title: 'Xóa món này?',
        text: "Bạn có chắc muốn bỏ món này khỏi giỏ hàng?",
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        cancelButtonColor: '#cbd5e1',
        confirmButtonText: 'Đồng ý xóa',
        cancelButtonText: 'Giữ lại'
    }).then((result) => {
        if (result.isConfirmed) {
            fetch(`/Cart/Remove?id=${id}`, { method: 'POST' })
                .then(res => res.json())
                .then(data => {
                    if (data.success) loadCartSidebar();
                });
        }
    });
}

// Tự động load giỏ hàng khi mở web
document.addEventListener('DOMContentLoaded', loadCartSidebar);


// ====================================================
// 6. XỬ LÝ VOUCHER (GIẢM GIÁ)
// ====================================================
function applyVoucher() {
    const code = document.getElementById("voucherCode").value.trim();
    if(!code) {
        Swal.fire('Nhập mã', 'Vui lòng nhập mã giảm giá!', 'info');
        return;
    }
    
    fetch(`/Cart/ApplyVoucher?code=${code}`, { method: 'POST' })
        .then(res => res.json())
        .then(data => {
            const msg = document.getElementById('voucherMessage');
            if(!msg) return;

            msg.style.display = "block";
            msg.style.marginTop = "8px";
            msg.style.fontSize = "13px";
            msg.style.fontWeight = "600";

            if(data.success) {
                msg.style.color = "#10b981"; // Xanh ngọc
                msg.innerHTML = "🎉 Áp dụng mã thành công!";
                loadCartSidebar(); // Load lại để update giá mới
            } else {
                msg.style.color = "#ef4444"; // Đỏ
                msg.innerHTML = "❌ " + (data.message || "Mã không hợp lệ!");
            }
        });
}


// ====================================================
// 7. LIVE SEARCH (TÌM KIẾM TRỰC TIẾP TRÊN HEADER)
// ====================================================
let searchTimer = null;

document.addEventListener("DOMContentLoaded", () => {
    const searchInput = document.getElementById('liveSearchInput');
    const searchResults = document.getElementById('searchResults'); // ID mới chuẩn theo _Layout

    if(searchInput && searchResults) {
        // Bắt sự kiện khi người dùng gõ phím
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimer);
            const query = this.value.trim();

            if (query.length === 0) {
                searchResults.classList.remove('active');
                return;
            }

            // Hiện loading
            searchResults.classList.add('active');
            searchResults.innerHTML = '<div class="search-loading" style="padding:15px; text-align:center; color:#d97706; font-weight:600;"><i class="fa-solid fa-spinner fa-spin"></i> Đang tìm kiếm...</div>';

            // Debounce: Đợi 0.4s sau khi ngừng gõ mới gọi API
            searchTimer = setTimeout(() => {
                fetch(`/Home/SearchAjax?query=${encodeURIComponent(query)}`)
                    .then(response => response.json())
                    .then(data => {
                        renderQuickSearch(data, searchResults);
                    })
                    .catch(err => {
                        console.error("Lỗi tìm kiếm:", err);
                        searchResults.innerHTML = '<div class="search-empty" style="padding:15px; text-align:center; color:#ef4444; font-weight:600;">Lỗi kết nối mạng!</div>';
                    });
            }, 400); 
        });

        // Ẩn kết quả khi click ra ngoài
        document.addEventListener('click', function(e) {
            if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
                searchResults.classList.remove('active');
            }
        });
        
        // Hiện lại kết quả khi click lại vào ô search (nếu đã có chữ)
        searchInput.addEventListener('focus', function() {
            if (this.value.trim().length > 0 && searchResults.innerHTML.trim() !== "") {
                searchResults.classList.add('active');
            }
        });
    }

    // ====================================================
    // 8. ĐÓNG/MỞ USER DROPDOWN MENU
    // ====================================================
    const userMenuBtn = document.getElementById('userMenuBtn');
    const userDropdown = document.getElementById('userDropdown');

    if (userMenuBtn && userDropdown) {
        userMenuBtn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            userDropdown.classList.toggle('active');
        });

        // Đóng dropdown khi click ra ngoài
        document.addEventListener('click', function(e) {
            if (!userDropdown.contains(e.target) && !userMenuBtn.contains(e.target)) {
                userDropdown.classList.remove('active');
            }
        });
    }
});

// Hàm Render HTML riêng cho Dropdown tìm kiếm
function renderQuickSearch(data, container) {
    if (data.length === 0) {
        container.innerHTML = '<div style="padding: 20px; text-align: center; color: #888; font-size: 14px; font-weight:600;">😭 Không tìm thấy món nào!</div>';
        return;
    }

    let html = '';
    // Lấy tối đa 6 kết quả
    const displayData = data.slice(0, 6);

    displayData.forEach(item => {
        let imgSrc = (item.image && item.image.startsWith("http")) ? item.image : `/Images/${item.image || 'default.png'}`;

        html += `
            <a href="/Home/Detail/${item.id}" class="search-item" style="display: flex; align-items: center; padding: 12px 16px; gap: 15px; text-decoration: none; border-bottom: 1px solid #f3f4f6; transition: 0.2s;" onmouseover="this.style.backgroundColor='#fef3c7'" onmouseout="this.style.backgroundColor='transparent'">
                <img src="${imgSrc}" onerror="this.src='/Images/default.png'" style="width: 50px; height: 50px; border-radius: 10px; object-fit: cover; border: 1px solid #fef3c7;" />
                <div class="search-item-info" style="flex: 1;">
                    <div style="font-weight: 800; font-size: 0.95rem; margin-bottom: 4px; color:#4b2e2e;">${item.name}</div>
                    <div style="color: #d97706; font-weight: 700; font-size: 0.85rem;">${item.price.toLocaleString()}đ</div>
                </div>
            </a>
        `;
    });

    if (data.length > 6) {
        html += `
            <div onclick="window.location.href='/Home/Index'" style="padding: 12px; text-align: center; background: #fffbeb; color: #d97706; font-weight: 800; cursor: pointer; font-size: 13px; transition: 0.2s;" onmouseover="this.style.backgroundColor='#fde68a'" onmouseout="this.style.backgroundColor='#fffbeb'">
                Xem tất cả ${data.length} kết quả <i class="fa-solid fa-arrow-right" style="margin-left: 5px;"></i>
            </div>
        `;
    }

    container.innerHTML = html;
}