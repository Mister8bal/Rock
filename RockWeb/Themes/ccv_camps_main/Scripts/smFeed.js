Vue.component('live-feed', {
  props: ['limit', 'url'],
  template: '<section class="posts">\n  <template v-for="post in posts">\n    <template v-if="post.kind == \'quote\'"> <quote-post :post=post></quote-post> </template>\n    <template v-if="post.kind == \'image\'"> <image-post :post=post></image-post> </template>\n    <template v-if="post.kind == \'video\'"> <video-post :post=post></video-post> </template>\n  </template>\n</section>',
  data: function() {
    return {
      posts: null
    };
  },
  created: function() {
    return this.fetchData();
  },
  methods: {
    fetchData: function() {
      return $.getJSON(this.apiURL, (function(_this) {
        return function(data) {
          return _this.posts = data;
        };
      })(this));
    }
  },
  computed: {
    apiURL: function() {
      var url;
      url = this.url;
      if (this.limit) {
        url = url + "?limit=" + this.limit;
      }
      return url;
    }
  }
});

Vue.component('quote-post', {
  props: ['post'],
  template: '<div class="post post-quote">\n  <div class="post-text">{{ post.text }}</div>\n  <attribution :post=post></attribution>\n</div>'
});

Vue.component('image-post', {
  props: ['post'],
  template: '<div class="post post-image">\n  <a class="post-media" :href="post.url" title="View post" v-html="post.media_tag"></a>\n  <div class="post-text">{{ post.text }}</div>\n  <attribution :post=post></attribution>\n</div>'
});

Vue.component('video-post', {
  props: ['post'],
  template: '<div class="post post-video">\n  <div class="post-media post-video-wrapper" v-html="post.media_tag"></div>\n  <div class="post-text">{{ post.text }}</div>\n  <attribution :post=post></attribution>\n</div>'
});

Vue.component('attribution', {
  props: ['post'],
  computed: {
    profileLink: function() {
      switch (false) {
        case this.post.type !== 'TwitterPost':
          return "//twitter.com/" + this.post.user_name;
        case this.post.type !== 'InstagramPost':
          return "//instagram.com/" + this.post.user_name;
        default:
          return '';
      }
    },
    date: function() {
      return moment(this.post.published_at).fromNow();
    }
  },
  template: '<div class="attribution">\n  <a :href="profileLink" class="avatar" title="View profile">\n    <img :src="post.user_photo" :alt="post.user_name" />\n  </a>\n  <div class="post-info">\n    <a :href="profileLink" class="name" title="View profile">\n      <strong>{{ post.user_full_name }}</strong><br>\n      @{{ post.user_name }}\n    </a>\n    <div class="date">\n      <a :href="post.url" title="View post">{{ date }}</a>\n    </div>\n  </div>\n</div>'
});

$(function() {

  $('.js-vue').each(function() {
    return new Vue({
      el: this
    });
  });

  return $('.jump-up').click(function() {
    $('html,body').animate({
      scrollTop: 0
    }, 300);
    return false;
  });

});

$(window).load(function(){

    function detectmob() { 
      if( navigator.userAgent.match(/Android/i)
        || navigator.userAgent.match(/webOS/i)
        || navigator.userAgent.match(/iPhone/i)
        || navigator.userAgent.match(/iPad/i)
        || navigator.userAgent.match(/iPod/i)
        || navigator.userAgent.match(/BlackBerry/i)
        || navigator.userAgent.match(/Windows Phone/i)
      ){
          return true;
      }else {
          return false;
      }
    }

    //Add an additional timeout to make sure feed has loaded.
    setTimeout(function(){

      var mod =  $('.post').length;
      var h = 200;

      if(!detectmob()){
          h = Math.ceil(mod / 3)  * 55 ;
        }else{
            h = mod * 55 ;
        }
      $('.posts').css('height', h +"vh");

    },500);

});