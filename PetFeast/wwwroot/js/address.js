const PROVINCES_URL = "/data/provinces.json";
const WARDS_URL = "/data/wards.json";


/*
|--------------------------------------------------------------------------
| DỮ LIỆU
|--------------------------------------------------------------------------
*/

let provinces = [];
let wards = [];


/*
|--------------------------------------------------------------------------
| KHỞI TẠO
|--------------------------------------------------------------------------
*/

async function loadAddressData(options = {}) {

    const {
        provinceSelectId = "province",
        wardSelectId = "ward",

        cityValueId = "cityValue",
        wardValueId = "wardValue",

        currentCity = "",
        currentWard = ""
    } = options;


    const provinceSelect =
        document.getElementById(provinceSelectId);

    const wardSelect =
        document.getElementById(wardSelectId);

    const cityValue =
        document.getElementById(cityValueId);

    const wardValue =
        document.getElementById(wardValueId);


    if (!provinceSelect || !wardSelect) {

        console.error(
            "Không tìm thấy #province hoặc #ward."
        );

        return;
    }


    try {

        /*
        |--------------------------------------------------------------------------
        | LOAD 2 FILE JSON
        |--------------------------------------------------------------------------
        */

        const [
            provincesResponse,
            wardsResponse
        ] = await Promise.all([

            fetch(PROVINCES_URL),

            fetch(WARDS_URL)

        ]);


        if (!provincesResponse.ok) {

            throw new Error(
                "Không thể tải provinces.json"
            );
        }


        if (!wardsResponse.ok) {

            throw new Error(
                "Không thể tải wards.json"
            );
        }


        provinces =
            await provincesResponse.json();


        wards =
            await wardsResponse.json();


        /*
        |--------------------------------------------------------------------------
        | LOAD TỈNH / THÀNH PHỐ
        |--------------------------------------------------------------------------
        */

        provinceSelect.innerHTML = `
            <option value="">
                -- Chọn Tỉnh / Thành phố --
            </option>
        `;


        provinces.forEach(province => {

            const option =
                document.createElement("option");


            option.value =
                province.id;


            option.textContent =
                province.name.local;


            provinceSelect.appendChild(option);

        });


        provinceSelect.disabled = false;


        /*
        |--------------------------------------------------------------------------
        | NẾU LÀ EDIT
        |--------------------------------------------------------------------------
        */

        if (currentCity) {

            const oldProvince =
                provinces.find(province =>

                    normalizeAddressText(
                        province.name.local
                    )
                    ===
                    normalizeAddressText(
                        currentCity
                    )

                );


            if (oldProvince) {

                provinceSelect.value =
                    oldProvince.id;


                if (cityValue) {

                    cityValue.value =
                        oldProvince.name.local;

                }


                loadWards(
                    oldProvince.id,
                    currentWard
                );
            }
        }


    }
    catch (error) {

        console.error(
            "Lỗi tải dữ liệu địa chỉ:",
            error
        );


        provinceSelect.innerHTML = `
            <option value="">
                Không thể tải dữ liệu địa chỉ
            </option>
        `;

        provinceSelect.disabled = true;

    }


    /*
    |--------------------------------------------------------------------------
    | CHỌN TỈNH
    |--------------------------------------------------------------------------
    */

    provinceSelect.addEventListener(
        "change",
        function () {

            const provinceId =
                this.value;


            const selectedOption =
                this.options[
                this.selectedIndex
                ];


            /*
            | Lưu tên tỉnh vào City
            */

            if (cityValue) {

                cityValue.value =
                    selectedOption &&
                        selectedOption.value
                        ? selectedOption.text
                        : "";

            }


            /*
            | Reset phường/xã
            */

            resetWard();


            if (!provinceId) {

                return;
            }


            loadWards(
                provinceId
            );

        }
    );


    /*
    |--------------------------------------------------------------------------
    | CHỌN PHƯỜNG / XÃ
    |--------------------------------------------------------------------------
    */

    wardSelect.addEventListener(
        "change",
        function () {

            const selectedOption =
                this.options[
                this.selectedIndex
                ];


            if (wardValue) {

                wardValue.value =
                    selectedOption &&
                        selectedOption.value
                        ? selectedOption.text
                        : "";

            }

        }
    );


    /*
    |--------------------------------------------------------------------------
    | HÀM RESET PHƯỜNG / XÃ
    |--------------------------------------------------------------------------
    */

    function resetWard() {

        wardSelect.innerHTML = `
            <option value="">
                -- Chọn Phường / Xã --
            </option>
        `;


        wardSelect.disabled = true;


        if (wardValue) {

            wardValue.value = "";

        }

    }


    /*
    |--------------------------------------------------------------------------
    | LOAD PHƯỜNG / XÃ
    |--------------------------------------------------------------------------
    */

    function loadWards(
        provinceId,
        selectedWard = ""
    ) {

        /*
        | Lọc toàn bộ xã/phường theo tỉnh
        */

        const provinceWards =
            wards.filter(ward =>

                String(
                    ward.parent?.id
                )
                ===
                String(provinceId)

            );


        /*
        | Reset
        */

        wardSelect.innerHTML = `
            <option value="">
                -- Chọn Phường / Xã --
            </option>
        `;


        /*
        | Không có dữ liệu
        */

        if (provinceWards.length === 0) {

            wardSelect.innerHTML = `
                <option value="">
                    Không có dữ liệu phường / xã
                </option>
            `;

            wardSelect.disabled = true;

            return;
        }


        /*
        | Thêm danh sách xã/phường
        */

        provinceWards.forEach(ward => {

            const option =
                document.createElement("option");


            option.value =
                ward.id;


            option.textContent =
                ward.name.local;


            wardSelect.appendChild(option);

        });


        wardSelect.disabled = false;


        /*
        |--------------------------------------------------------------------------
        | EDIT ADDRESS
        |--------------------------------------------------------------------------
        */

        if (selectedWard) {

            const oldWard =
                provinceWards.find(ward =>

                    normalizeAddressText(
                        ward.name.local
                    )
                    ===
                    normalizeAddressText(
                        selectedWard
                    )

                );


            if (oldWard) {

                wardSelect.value =
                    oldWard.id;


                if (wardValue) {

                    wardValue.value =
                        oldWard.name.local;

                }

            }

        }

    }

}


/*
|--------------------------------------------------------------------------
| CHUẨN HÓA TÊN ĐỊA CHỈ
|--------------------------------------------------------------------------
|
| Giúp Edit nhận ra:
|
| "Xuân Hương - Đà Lạt"
| "xuân hương - đà lạt"
| " Xuân Hương - Đà Lạt "
|
| là cùng một giá trị.
|
*/

function normalizeAddressText(text) {

    if (!text) {

        return "";

    }


    return text
        .toString()
        .trim()
        .normalize("NFC")
        .replace(/\s+/g, " ")
        .toLowerCase();

}