UMC.UI.Config({ 'posurl': '/UMC/' + (UMC.cookie('device') || UMC.cookie('device', UMC.uuid())) });
UMC.Src = '//www.365lu.cn/v0.1/';
UMC.SPA = '/';

UMC(function ($) {
    var html = [];

    html.push('<li><a ui-spa href="/dashboard">工作台</a></li>',
        '<li><a ui-spa href="/explore">发现</a></li>',
        '<li><a ui-spa href="/365lu/help">功能说明</a></li>',
        '<li><a class="AppDown" ui-spa href="/download">App下载</a></li>',
        '<li><a ui-spa href="/365lu/open">开放开源</a></li>');

    var site = $('.header-sub-nav .menu-site').html(html.join(''));
    requestAnimationFrame(() => {
        function ns() {
            var path = location.pathname;
            site.find('li').cls('is-active', 0).find('a').each(function () {
                var m = $(this);
                var s = m.attr('ui-spa');

                var k = s ? ($.SPA + s) : m.attr('href');
                if (k == path) {
                    m.parent().cls('is-active', 1);
                    return false;
                }
            });
        };

        $(window).on("popstate", ns);
        ns();
    });

});