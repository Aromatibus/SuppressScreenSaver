fn main() {
    // Windowsターゲットの場合のみリソースをコンパイルします
    if std::env::var("CARGO_CFG_TARGET_OS").unwrap() == "windows" {
        // 第2引数にマクロ定義（不要な場合は NONE）を指定します
        embed_resource::compile("resource.rc", embed_resource::NONE);
    }
}
